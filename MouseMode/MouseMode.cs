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
public class MouseMode : IPositionedPipelineElement<IDeviceReport>
{
    [Property(RESET_TIME_NAME), Unit("ms"), 
     DefaultPropertyValue(RESET_TIME_DEFAULT), 
     ToolTip("Time in milliseconds between reports before retaining mouse position and resetting canvas coordinates\n" 
             + "- Negative value means never reset\n" 
             + "- Zero means always reset\n")]
    public int ResetTime
    {
        set => MouseModeProperties.SetDefault(RESET_TIME_INDEX, value);
    }

    [BooleanProperty(IGNORE_OOB_TABLET_INPUT_NAME, ""),
     DefaultPropertyValue(IGNORE_OOB_TABLET_INPUT_DEFAULT)]
    public bool bIgnoreOobTabletInput
    {
        set => MouseModeProperties.SetDefault(IGNORE_OOB_TABLET_INPUT_INDEX, value);
    }

    [BooleanProperty(NORMALIZE_ASPECT_RATIO_NAME, ""),
     DefaultPropertyValue(NORMALIZE_ASPECT_RATIO_DEFAULT)]
    public bool bNormalizeAspectRatio
    {
        set => MouseModeProperties.SetDefault(NORMALIZE_ASPECT_RATIO_INDEX, value);
    }

    [Property(SPEED_MULTIPLIER_NAME),
     DefaultPropertyValue(SPEED_MULTIPLIER_DEFAULT)]
    public float SpeedMultiplier
    {
        set => MouseModeProperties.SetDefault(SPEED_MULTIPLIER_INDEX, value);
    }

    [BooleanProperty(ACCELERATION_ENABLED_NAME, ""),
     DefaultPropertyValue(ACCELERATION_ENABLED_DEFAULT)]
    public bool bAccelerationEnabled
    {
        set => MouseModeProperties.SetDefault(ACCELERATION_ENABLED_INDEX, value);
    }

    [Property(ACCELERATION_INTENSITY_NAME),
     DefaultPropertyValue(ACCELERATION_INTENSITY_DEFAULT)]
    public float AccelerationIntensity
    {
        set => MouseModeProperties.SetDefault(ACCELERATION_INTENSITY_INDEX, value);
    }
    
    [TabletReference] 
    public TabletReference TabletRef { get; set; }

    [Resolved] 
    public IDriver DriverRef { get; set; }
    
    // internal variables
    private TimeSpan ResetTimeSpan;
    private bool _bIgnoreOobTabletInput;
    private bool _bNormalizeAspectRatio;
    private float _speedMultiplier;
    private bool _bAccelerationEnabled;
    private float _accelerationIntensity;

    private HPETDeltaStopwatch Stopwatch = new HPETDeltaStopwatch(true);

    private DigitizerSpecifications Digitizer;
    private Vector2 MaxDigitizerCoords;
    private Vector2 DigitizerSize;

    private AbsoluteOutputMode? OutputMode = null;
    private bool bIgnoreOobWorkAreaInput;
    private bool bClampOobWorkAreaInput;
    
    private Matrix3x2 OutputTransformMatrix;
    private Matrix3x2 OutputInverseTransformMatrix;

    private float CanvasRotation;
    private Vector2 CanvasOrigin;
    private Vector2 MinInputCoords;
    private Vector2 MaxOutputCoords;
    
    private Matrix3x2 AspectRatioNormalizationMatrix;
    private Vector2 TabletToPhysicialCoordRatio;
    
    private Vector2 LastPos;
    private Vector2 LastLocalPos;

    public MouseMode()
    {
        MouseModeProperties.GetProperty<int>(RESET_TIME_INDEX)!.OnValueChanged += value => ResetTimeSpan = TimeSpan.FromMilliseconds(value);
        MouseModeProperties.GetProperty<bool>(IGNORE_OOB_TABLET_INPUT_INDEX)!.OnValueChanged += value => _bIgnoreOobTabletInput = value;
        MouseModeProperties.GetProperty<bool>(NORMALIZE_ASPECT_RATIO_INDEX)!.OnValueChanged += value => _bNormalizeAspectRatio = value;
        MouseModeProperties.GetProperty<float>(SPEED_MULTIPLIER_INDEX)!.OnValueChanged += value => _speedMultiplier = value;
        MouseModeProperties.GetProperty<bool>(ACCELERATION_ENABLED_INDEX)!.OnValueChanged += value => _bAccelerationEnabled = value;
        MouseModeProperties.GetProperty<float>(ACCELERATION_INTENSITY_INDEX)!.OnValueChanged += value => _accelerationIntensity = value;
    }

    [OnDependencyLoad]
    public void Recompile()
    {
        OutputMode = null;
    }
    
    public void Consume(IDeviceReport report)
    {
        if(report is OutOfRangeReport)
            return;

        var newPosition = Vector2.Zero;
        if (report is ITabletReport tabletReport)
        {
            newPosition = tabletReport.Position;
        }
        else
        {
            Emit?.Invoke(report);
            return;
        }

        if (OutputMode == null && !TryGetOutputMode())
        {
            Emit?.Invoke(report);
            return;
        }
        
        // clamp and drop invalid reports
        newPosition = ClampInput(newPosition);
        if(newPosition == Vector2.Zero)
            return;

        // check for reset timeout
        var deltaTime = Stopwatch.Restart();
        if (ResetTimeSpan.TotalSeconds >= 0 && deltaTime >= ResetTimeSpan)
        {
            ResetCanvas(newPosition);
        }

        // calculate, scale and apply displacement
        var displacement = ScaleDisplacement(newPosition - LastPos, (float)deltaTime.TotalSeconds);
        var transformedPos = ClampOutput(LastLocalPos + CanvasOrigin + displacement);

        // save final coordinates and report
        tabletReport.Position = transformedPos;
        Emit?.Invoke(tabletReport);

        LastPos = newPosition;
        LastLocalPos = transformedPos - CanvasOrigin;
    }

    public event Action<IDeviceReport>? Emit;
    public PipelinePosition Position => PipelinePosition.PreTransform;

    private bool TryGetOutputMode()
    {
        if (DriverRef is not Driver drv)
            return false;
        
        OutputMode = (AbsoluteOutputMode?)drv.InputDevices
            .Where(device => device?.OutputMode?.Elements?.Contains(this) ?? false)
            .Select(device => device?.OutputMode)
            .FirstOrDefault(outputMode => outputMode is AbsoluteOutputMode);
        
        if (OutputMode == null)
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
        OutputTransformMatrix = OutputMode.TransformationMatrix;
        Matrix3x2.Invert(OutputTransformMatrix, out OutputInverseTransformMatrix);
        
        // tablet digitizer variables
        Digitizer = TabletRef.Properties.Specifications.Digitizer;
        DigitizerSize = new Vector2(Digitizer.Width, Digitizer.Height);
        MaxDigitizerCoords = new Vector2(Digitizer.MaxX, Digitizer.MaxY);
        
        TabletToPhysicialCoordRatio = DigitizerSize / MaxDigitizerCoords;
        
        // input clamping
        bIgnoreOobWorkAreaInput = OutputMode.AreaLimiting;
        bClampOobWorkAreaInput = OutputMode.AreaClipping || bIgnoreOobWorkAreaInput;
        
        // input coordinate space variables
        var canvasSize = new Vector2(OutputMode.Input.Width, OutputMode.Input.Height);
        MinInputCoords = OutputMode.Input.Position - canvasSize / 2;
        MaxOutputCoords = MinInputCoords + canvasSize;

        // transform to digitizer coordinate space
        MinInputCoords /= TabletToPhysicialCoordRatio;
        MaxOutputCoords /= TabletToPhysicialCoordRatio;
        
        // canvas rotation
        CanvasRotation = OutputMode.Input.Rotation;

        // aspect ratio normalization
        AspectRatioNormalizationMatrix = _bNormalizeAspectRatio ? GetAspectRatioNormalizationMatrix() : Matrix3x2.Identity;
        
        // initialize relative position variables
        LastLocalPos = Vector2.Zero;
        
        CanvasOrigin = OutputMode.Output.Position; // center of display area
        CanvasOrigin = Vector2.Transform(CanvasOrigin, OutputInverseTransformMatrix); // transform to input space
    }

    private Vector2 ClampInput(Vector2 pos)
    {
        if (_bIgnoreOobTabletInput && !IsWithin(pos, Vector2.Zero, MaxDigitizerCoords))
        {
            return Vector2.Zero;
        }

        if (bClampOobWorkAreaInput && !IsWithin(pos, MinInputCoords, MaxOutputCoords, CanvasRotation))
        {
            if (bIgnoreOobWorkAreaInput)
            {
                return Vector2.Zero;
            }
            return Clamp(pos, MinInputCoords, MaxOutputCoords, CanvasRotation);
        }
        
        return pos;
    }

    private Vector2 ClampOutput(Vector2 pos)
    {
        if (!IsWithin(pos, MinInputCoords, MaxOutputCoords))
        {
            return Clamp(pos, MinInputCoords, MaxOutputCoords, CanvasRotation);
        }
        
        return pos;
    }

    private void ResetCanvas(Vector2 currentPosition)
    {
        LastPos = currentPosition;
        CanvasOrigin = LastLocalPos + CanvasOrigin;
        LastLocalPos = Vector2.Zero;
    }

    private Vector2 ScaleDisplacement(Vector2 displacement, float deltaTime)
    {
        float velocity = (displacement * TabletToPhysicialCoordRatio / 25.4f).Length() / deltaTime; // convert mm/s to inch/s
        float accelerationMultiplier = 1f;
        if (_bAccelerationEnabled)
        {
            accelerationMultiplier = MouseAcceleration.GetMultiplier(velocity) * (float)Math.Sqrt(_accelerationIntensity) / 8; // divide to compensate for larger pen movement compared to mouse;
        }
        
        return  Vector2.Transform(displacement, AspectRatioNormalizationMatrix) * accelerationMultiplier * _speedMultiplier;
    }

    private Matrix3x2 GetAspectRatioNormalizationMatrix()
    {
        var transformMat = OutputMode.TransformationMatrix with { M31 = 0, M32 = 0 }; // get output linear transform
        Matrix3x2.Invert(transformMat, out var invTransform);

        var inputArea = OutputMode.Input;
        var outputArea = OutputMode.Output;

        var inputAspectRatio = inputArea.Width / inputArea.Height;
        var outputAspectRatio = outputArea.Width / outputArea.Height;
        Vector2 scale = Vector2.Normalize(new Vector2(inputAspectRatio/outputAspectRatio, 1f)); // correction in output space
        
        var correctionMatrix = transformMat * Matrix3x2.CreateScale(scale) * invTransform; // transfer correction from output to input space

        return correctionMatrix;
    }
}