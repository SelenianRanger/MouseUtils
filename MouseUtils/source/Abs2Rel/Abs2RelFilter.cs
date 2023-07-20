using System.Numerics;
using MouseUtils.WindowsAcceleration;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;
using static MouseUtils.Extensions;
using static MouseUtils.Constants;

namespace MouseUtils.Abs2Rel;

[PluginName("Abs2Rel")]
public class Abs2RelFilter : FilterBase
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

    // internal variables
    private TimeSpan _resetTimeSpan;
    private bool _bIgnoreOobTabletInput;
    private bool _bNormalizeAspectRatio;
    private float _speedMultiplier;
    private float _accelerationIntensity;

    private readonly HPETDeltaStopwatch _stopwatch = new HPETDeltaStopwatch(true);
    
    private bool _bIgnoreOobWorkAreaInput;
    private bool _bClampOobWorkAreaInput;

    private float _canvasRotation;
    private Vector2 _canvasOrigin;
    private Vector2 _minInputCoords;
    private Vector2 _maxOutputCoords;
    private Vector2 _maxDigitizerCoords;

    private Matrix3x2 _aspectRatioNormalizationMatrix;
    private Vector2 _tabletToPhysicalCoordRatio;
    
    private Vector2 _lastPos;
    private Vector2 _lastLocalPos;

    protected override void OnValidReport(IDeviceReport report)
    {
        var tabletReport = report as ITabletReport;
        var newPosition= tabletReport!.Position;

        // manually set to absolute mode
        if (_resetTimeSpan < TimeSpan.Zero)
        {
            ResetCanvas(newPosition, true);
            InvokeEmit(report);
            return; 
        }

        // clamp and drop invalid reports
        newPosition = ClampInput(newPosition);
        if (newPosition == Vector2.Zero)
        {
            tabletReport.Position = _lastLocalPos + _canvasOrigin;
            InvokeEmit(tabletReport);
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
        InvokeEmit(tabletReport);

        _lastPos = newPosition;
        _lastLocalPos = transformedPos - _canvasOrigin;
    }

    protected override void SubscribeToPropertyChanges()
    {
        _properties.GetProperty<int>(PropertyIndex.ResetTime)!.OnValueChanged += (_, value) => _resetTimeSpan = TimeSpan.FromMilliseconds(value);
        _properties.GetProperty<bool>(PropertyIndex.IgnoreOobTabletInput)!.OnValueChanged += (_, value) => _bIgnoreOobTabletInput = value;
        _properties.GetProperty<bool>(PropertyIndex.NormalizeAspectRatio)!.OnValueChanged += (_, value) => _bNormalizeAspectRatio = value;
        _properties.GetProperty<float>(PropertyIndex.SpeedMultiplier)!.OnValueChanged += (_, value) => _speedMultiplier = value;
        _properties.GetProperty<float>(PropertyIndex.AccelerationIntensity)!.OnValueChanged += (_, value) => _accelerationIntensity = value;
    }

    protected override void InitializeProperties()
    {
        _properties.SetDefault(PropertyIndex.ResetTime, ResetTime);
        _properties.SetDefault(PropertyIndex.IgnoreOobTabletInput, bIgnoreOobTabletInput);
        _properties.SetDefault(PropertyIndex.NormalizeAspectRatio, bNormalizeAspectRatio);
        _properties.SetDefault(PropertyIndex.SpeedMultiplier, SpeedMultiplier);
    }

    protected override void OnOutputModeFound()
    {
        if (OutputMode is not AbsoluteOutputMode absoluteOutputMode)
        {
            Log.WriteNotify("Abs2Rel", "Abs2Rel requires an absolute output mode", LogLevel.Error);
            return;
        }
        
        var digitizer = TabletRef.Properties.Specifications.Digitizer;

        // input clamping
        _bIgnoreOobWorkAreaInput = absoluteOutputMode.AreaLimiting;
        _bClampOobWorkAreaInput = absoluteOutputMode.AreaClipping || _bIgnoreOobWorkAreaInput;
        
        // canvas rotation
        _canvasRotation = absoluteOutputMode.Input.Rotation;
        
        // input coordinate space variables
        var canvasSize = new Vector2(absoluteOutputMode.Input.Width, absoluteOutputMode.Input.Height);
        _minInputCoords = absoluteOutputMode.Input.Position - canvasSize / 2;
        _maxOutputCoords = _minInputCoords + canvasSize;
        _maxDigitizerCoords = new Vector2(digitizer.MaxX, digitizer.MaxY);
        
        _tabletToPhysicalCoordRatio = GetTabletToPhysicalRatio(TabletRef);

        // transform to digitizer coordinate space
        _minInputCoords /= _tabletToPhysicalCoordRatio;
        _maxOutputCoords /= _tabletToPhysicalCoordRatio;

        // aspect ratio normalization
        _aspectRatioNormalizationMatrix = _bNormalizeAspectRatio ? GetAspectRatioNormalizationMatrix() : Matrix3x2.Identity;
        
        // initialize relative position variables
        _lastLocalPos = Vector2.Zero;

        Matrix3x2.Invert(absoluteOutputMode.TransformationMatrix, out var invTransformMat);
        _canvasOrigin = absoluteOutputMode.Output.Position; // center of display area
        _canvasOrigin = Vector2.Transform(_canvasOrigin, invTransformMat); // transform to input space
    }

    protected override bool IsValidReport(IDeviceReport report)
    {
        if (!base.IsValidReport(report))
            return false;

        if (OutputMode is not AbsoluteOutputMode)
            return false;

        if (report is not ITabletReport)
            return false;

        return true;
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
        if (_accelerationIntensity != 0)
        {
            accelerationMultiplier = WindowsMouseAcceleration.GetMultiplier(velocity) * (float)Math.Sqrt(_accelerationIntensity);
        }
        
        return  Vector2.Transform(displacement, _aspectRatioNormalizationMatrix) * accelerationMultiplier * _speedMultiplier;
    }

    private Matrix3x2 GetAspectRatioNormalizationMatrix()
    {
        var absOutputMode = OutputMode as AbsoluteOutputMode;
        
        var transformMat = absOutputMode!.TransformationMatrix with { M31 = 0, M32 = 0 }; // get output linear transform
        Matrix3x2.Invert(transformMat, out var invTransform);

        var inputArea = absOutputMode.Input;
        var outputArea = absOutputMode.Output;

        var inputAspectRatio = inputArea.Width / inputArea.Height;
        var outputAspectRatio = outputArea.Width / outputArea.Height;
        Vector2 scale = Vector2.Normalize(new Vector2(inputAspectRatio/outputAspectRatio, 1f)); // correction in output space
        
        var correctionMatrix = transformMat * Matrix3x2.CreateScale(scale) * invTransform; // transfer correction from output to input space

        return correctionMatrix;
    }
}