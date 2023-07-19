using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;
using static MouseMode.MouseModeExtensions;
using static MouseMode.MouseModeConstants;

namespace MouseMode;

[PluginName("MouseMode")]
public class MouseMode : IPositionedPipelineElement<IDeviceReport>, IDisposable
{
    [Property(RESET_TIME_NAME), Unit("ms"), 
     DefaultPropertyValue(RESET_TIME_DEFAULT), 
     ToolTip("Time in milliseconds between reports before retaining mouse position and resetting canvas coordinates\n" 
             + "- Zero means never reset\n" 
             + "- Negative values reverts input to absolute mode\n")]
    public int ResetTime { get; set; }

    [BooleanProperty(IGNORE_OOB_TABLET_INPUT_NAME, ""),
     DefaultPropertyValue(IGNORE_OOB_TABLET_INPUT_DEFAULT)]
    public bool bIgnoreOobTabletInput { get; set; }
    
    [BooleanProperty(NORMALIZE_ASPECT_RATIO_NAME, ""),
     DefaultPropertyValue(NORMALIZE_ASPECT_RATIO_DEFAULT)]
    public bool bNormalizeAspectRatio { get; set; }

    [Property(SPEED_MULTIPLIER_NAME),
     DefaultPropertyValue(SPEED_MULTIPLIER_DEFAULT)]
    public float SpeedMultiplier { get; set; }

    [BooleanProperty(ACCELERATION_ENABLED_NAME, ""),
     DefaultPropertyValue(ACCELERATION_ENABLED_DEFAULT)]
    public bool bAccelerationEnabled { get; set; }

    [Property(ACCELERATION_INTENSITY_NAME),
     DefaultPropertyValue(ACCELERATION_INTENSITY_DEFAULT)]
    public float AccelerationIntensity { get; set; }

    [TabletReference]
    public TabletReference TabletRef
    {
        get => _tabletRef;
        set
        {
            _tabletRef = value;
            _properties = MouseModeProperties.GetOrAddProperties(value, out var found);
            SubscribeToPropertyChanges();
        }
    }

    private TabletReference _tabletRef;
    
    [Resolved] 
    public IDriver DriverRef { get; set; }

    // internal variables
    private MouseModeProperties _properties;
    
    private TimeSpan _resetTimeSpan;
    private bool _bIgnoreOobTabletInput;
    private bool _bNormalizeAspectRatio;
    private float _speedMultiplier;
    private bool _bAccelerationEnabled;
    private float _accelerationIntensity;

    private HPETDeltaStopwatch _stopwatch = new HPETDeltaStopwatch(true);

    private DigitizerSpecifications _digitizer;
    private Vector2 _maxDigitizerCoords;
    private Vector2 _digitizerSize;

    private AbsoluteOutputMode? _outputMode;
    private bool _bIgnoreOobWorkAreaInput;
    private bool _bClampOobWorkAreaInput;
    
    private Matrix3x2 _outputTransformMatrix;
    private Matrix3x2 _outputInverseTransformMatrix;

    private float _canvasRotation;
    private Vector2 _canvasOrigin;
    private Vector2 _minInputCoords;
    private Vector2 _maxOutputCoords;
    
    private Matrix3x2 _aspectRatioNormalizationMatrix;
    private Vector2 _tabletToPhysicalCoordRatio;
    
    private Vector2 _lastPos;
    private Vector2 _lastLocalPos;

    [OnDependencyLoad]
    public void Recompile()
    {
        _outputMode = null;
        
        InitializeProperties();
    }

    public void Consume(IDeviceReport report)
    {
        if (!IsValidReport(report, out var tabletReport))
        {
            Emit?.Invoke(report);
            return; 
        }
        
        Vector2 newPosition= tabletReport.Position;

        // manually set to absolute mode
        if (_resetTimeSpan < TimeSpan.Zero)
        {
            ResetCanvas(newPosition, true);
            Emit?.Invoke(report);
            return; 
        }

        // clamp and drop invalid reports
        newPosition = ClampInput(newPosition);
        if (newPosition == Vector2.Zero)
        {
            tabletReport.Position = _lastLocalPos + _canvasOrigin;
            Emit?.Invoke(tabletReport);
            return;
        }

        // check for reset timeout
        var deltaTime = _stopwatch.Restart();
        if (_resetTimeSpan.TotalSeconds > 0 && deltaTime >= _resetTimeSpan)
        {
            ResetCanvas(newPosition);
        }

        // calculate, scale and apply displacement
        var displacement = ScaleDisplacement(newPosition - _lastPos, (float)deltaTime.TotalSeconds);
        var transformedPos = ClampOutput(_lastLocalPos + _canvasOrigin + displacement);
        
        // save final coordinates and report
        tabletReport.Position = transformedPos;
        Emit?.Invoke(tabletReport);

        _lastPos = newPosition;
        _lastLocalPos = transformedPos - _canvasOrigin;
    }

    public event Action<IDeviceReport>? Emit;
    public PipelinePosition Position => PipelinePosition.PreTransform;

    private void SubscribeToPropertyChanges()
    {
        _properties.GetProperty<int>(PropertyIndex.ResetTime)!.OnValueChanged += (_, value) => _resetTimeSpan = TimeSpan.FromMilliseconds(value);
        _properties.GetProperty<bool>(PropertyIndex.IgnoreOobTabletInput)!.OnValueChanged += (_, value) => _bIgnoreOobTabletInput = value;
        _properties.GetProperty<bool>(PropertyIndex.NormalizeAspectRatio)!.OnValueChanged += (_, value) => _bNormalizeAspectRatio = value;
        _properties.GetProperty<float>(PropertyIndex.SpeedMultiplier)!.OnValueChanged += (_, value) => _speedMultiplier = value;
        _properties.GetProperty<bool>(PropertyIndex.AccelerationEnabled)!.OnValueChanged += (_, value) => _bAccelerationEnabled = value;
        _properties.GetProperty<float>(PropertyIndex.AccelerationIntensity)!.OnValueChanged += (_, value) => _accelerationIntensity = value;
    }

    private void InitializeProperties()
    {
        _properties.SetDefault(PropertyIndex.ResetTime, ResetTime);
        _properties.SetDefault(PropertyIndex.IgnoreOobTabletInput, bIgnoreOobTabletInput);
        _properties.SetDefault(PropertyIndex.NormalizeAspectRatio, bNormalizeAspectRatio);
        _properties.SetDefault(PropertyIndex.SpeedMultiplier, SpeedMultiplier);
        _properties.SetDefault(PropertyIndex.AccelerationEnabled, bAccelerationEnabled);
        _properties.SetDefault(PropertyIndex.AccelerationIntensity, AccelerationIntensity);
    }

    private bool IsValidReport(IDeviceReport report,[NotNullWhen(true)] out ITabletReport? tabletReport)
    {
        tabletReport = null;
        // invalid report
        if (report is not ITabletReport newReport)
            return false;
        tabletReport = newReport;

        // no reference to output mode
        if (_outputMode == null && !TryGetOutputMode())
            return false;

        return true;
    }
    
    private bool TryGetOutputMode()
    {
        if (DriverRef is not Driver drv)
            return false;
        
        _outputMode = (AbsoluteOutputMode?)drv.InputDevices
            .Where(device => device?.OutputMode?.Elements?.Contains(this) ?? false)
            .Select(device => device?.OutputMode)
            .FirstOrDefault(outputMode => outputMode is AbsoluteOutputMode);
        
        if (_outputMode == null)
        {
            Log.WriteNotify("Mouse Mode", "Absolute output mode not found.", LogLevel.Error);
            throw new Exception("Absolute output mode not found");
        }

        InitializeVariables();

        return true;
    }

    private void InitializeVariables()
    {
        // output transform matrix
        _outputTransformMatrix = _outputMode.TransformationMatrix;
        Matrix3x2.Invert(_outputTransformMatrix, out _outputInverseTransformMatrix);
        
        // tablet digitizer variables
        _digitizer = TabletRef.Properties.Specifications.Digitizer;
        _digitizerSize = new Vector2(_digitizer.Width, _digitizer.Height);
        _maxDigitizerCoords = new Vector2(_digitizer.MaxX, _digitizer.MaxY);
        
        _tabletToPhysicalCoordRatio = _digitizerSize / _maxDigitizerCoords;
        
        // input clamping
        _bIgnoreOobWorkAreaInput = _outputMode.AreaLimiting;
        _bClampOobWorkAreaInput = _outputMode.AreaClipping || _bIgnoreOobWorkAreaInput;
        
        // input coordinate space variables
        var canvasSize = new Vector2(_outputMode.Input.Width, _outputMode.Input.Height);
        _minInputCoords = _outputMode.Input.Position - canvasSize / 2;
        _maxOutputCoords = _minInputCoords + canvasSize;

        // transform to digitizer coordinate space
        _minInputCoords /= _tabletToPhysicalCoordRatio;
        _maxOutputCoords /= _tabletToPhysicalCoordRatio;
        
        // canvas rotation
        _canvasRotation = _outputMode.Input.Rotation;

        // aspect ratio normalization
        _aspectRatioNormalizationMatrix = _bNormalizeAspectRatio ? GetAspectRatioNormalizationMatrix() : Matrix3x2.Identity;
        
        // initialize relative position variables
        _lastLocalPos = Vector2.Zero;
        
        _canvasOrigin = _outputMode.Output.Position; // center of display area
        _canvasOrigin = Vector2.Transform(_canvasOrigin, _outputInverseTransformMatrix); // transform to input space
    }
    
    private void ResetCanvas(Vector2 currentPosition, bool resetToAbsolute = false)
    {
        _lastPos = currentPosition;
        _canvasOrigin = resetToAbsolute ? currentPosition : _lastLocalPos + _canvasOrigin;
        _lastLocalPos = Vector2.Zero;
    }

    private Vector2 ClampInput(Vector2 pos)
    {
        if (_bIgnoreOobTabletInput && !IsWithin(pos, Vector2.Zero, _maxDigitizerCoords))
        {
            return Vector2.Zero;
        }

        if (_bClampOobWorkAreaInput && !IsWithin(pos, _minInputCoords, _maxOutputCoords, _canvasRotation))
        {
            if (_bIgnoreOobWorkAreaInput)
            {
                return Vector2.Zero;
            }
            return Clamp(pos, _minInputCoords, _maxOutputCoords, _canvasRotation);
        }
        
        return pos;
    }

    private Vector2 ClampOutput(Vector2 pos)
    {
        if (!IsWithin(pos, _minInputCoords, _maxOutputCoords))
        {
            return Clamp(pos, _minInputCoords, _maxOutputCoords, _canvasRotation);
        }
        
        return pos;
    }

    private Vector2 ScaleDisplacement(Vector2 displacement, float deltaTime)
    {
        float velocity = (displacement * _tabletToPhysicalCoordRatio / 25.4f).Length() / deltaTime; // convert mm/s to inch/s
        float accelerationMultiplier = 1f;
        if (_bAccelerationEnabled)
        {
            accelerationMultiplier = MouseAcceleration.GetMultiplier(velocity) * (float)Math.Sqrt(_accelerationIntensity) / 8; // divide to compensate for larger pen movement compared to mouse;
        }
        
        return  Vector2.Transform(displacement, _aspectRatioNormalizationMatrix) * accelerationMultiplier * _speedMultiplier;
    }

    private Matrix3x2 GetAspectRatioNormalizationMatrix()
    {
        var transformMat = _outputMode.TransformationMatrix with { M31 = 0, M32 = 0 }; // get output linear transform
        Matrix3x2.Invert(transformMat, out var invTransform);

        var inputArea = _outputMode.Input;
        var outputArea = _outputMode.Output;

        var inputAspectRatio = inputArea.Width / inputArea.Height;
        var outputAspectRatio = outputArea.Width / outputArea.Height;
        Vector2 scale = Vector2.Normalize(new Vector2(inputAspectRatio/outputAspectRatio, 1f)); // correction in output space
        
        var correctionMatrix = transformMat * Matrix3x2.CreateScale(scale) * invTransform; // transfer correction from output to input space

        return correctionMatrix;
    }

    public void Dispose()
    {
        _properties.UnsubscribeFromAll(this);
        GC.SuppressFinalize(this);
    }
}