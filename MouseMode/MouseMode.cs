using System.Numerics;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;
using static MouseMode.MouseModeStatics;

namespace MouseMode;

[PluginName("MouseMode")]
public class MouseMode : IPositionedPipelineElement<IDeviceReport>
{
    [Property("Reset Time"), Unit("ms"), DefaultPropertyValue(100), ToolTip(
         "Time in milliseconds between reports before retaining mouse position and resetting canvas coordinates\n" +
         "- Negative value means never reset\n" +
         "- Zero means always reset\n")]
    public int ResetTime { get; set; }

    [BooleanProperty("Drop Input Outside Full Tablet Area", ""), DefaultPropertyValue(true)]
    public bool bDropOutOfBoundsReports { get; set; }
    
    [BooleanProperty("Enable Custom Aspect Ratio", ""), DefaultPropertyValue(true)]
    public bool bCustomAspectRatioEnabled { get; set; }
    
    [Property("Custom Aspect Ratio X"), DefaultPropertyValue(16f)]
    public float CustomAspectRatioX { get; set; }
    
    [Property("Custom Aspect Ratio Y"), DefaultPropertyValue(9f)]
    public float CustomAspectRatioY { get; set; }
    
    [Property("Speed Multiplier"), DefaultPropertyValue(1f)]
    public float SpeedMultiplier { get; set; }

    [BooleanProperty("Use Windows Mouse Acceleration Curve", ""),
     DefaultPropertyValue(true)]
    public bool bMouseAccelerationEnabled { get; set; }

    [Property("Acceleration Intensity"), DefaultPropertyValue(1f)]
    public float AccelerationIntensity { get; set; }

    [TabletReference] 
    public TabletReference TabletRef { get; set; }

    [Resolved] 
    public IDriver DriverRef { get; set; }
    
    private DigitizerSpecifications Digitizer;
    private Vector2 MaxDigitizerCoords;
    private Vector2 DigitizerSize;

    private AbsoluteOutputMode? OutputMode = null;
    private bool bAbsIgnoreOobInput;
    private bool bAbsClampOobInput;
    
    private TimeSpan ResetTimeSpan;
    private HPETDeltaStopwatch Stopwatch = new HPETDeltaStopwatch(true);

    private Vector2 MinCoords;
    private Vector2 MaxCoords;
    private float CanvasRotation;
    private Vector2 CanvasOrigin;
    private Vector2 AspectRatioConversionRatio;
    private Vector2 TabletToPhysicialCoordRatio;

    private Vector2 LastPos;
    private Vector2 LastLocalPos;

    [OnDependencyLoad]
    public void Recompile()
    {
        OutputMode = null;

        ResetTimeSpan = new TimeSpan(0, 0, 0, 0, ResetTime);
        
        Digitizer = TabletRef.Properties.Specifications.Digitizer;
        DigitizerSize = new Vector2(Digitizer.Width, Digitizer.Height);
        MaxDigitizerCoords = new Vector2(Digitizer.MaxX, Digitizer.MaxY);
        TabletToPhysicialCoordRatio = DigitizerSize / MaxDigitizerCoords;

        AspectRatioConversionRatio = Vector2.Zero; // reset on next report

        CanvasOrigin = MaxDigitizerCoords / 2f;
        LastLocalPos = Vector2.Zero;
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
        if (ResetTime >= 0 && deltaTime >= ResetTimeSpan)
        {
            ResetCanvas(newPosition);
        }

        // calculate, scale and apply displacement
        var displacement = ScaleDisplacement(newPosition - LastPos, (float)deltaTime.TotalSeconds);
        var transformedPos = Vector2.Clamp( LastLocalPos + CanvasOrigin + displacement,
            Vector2.Zero, MaxDigitizerCoords);

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
        
        bAbsIgnoreOobInput = OutputMode.AreaLimiting;
        bAbsClampOobInput = OutputMode.AreaClipping || bAbsIgnoreOobInput;
        
        var canvasSize = new Vector2(OutputMode.Input.Width, OutputMode.Input.Height);
        MinCoords = OutputMode.Input.Position - canvasSize / 2;
        MaxCoords = MinCoords + canvasSize;

        // transform to digitizer coordinate space
        MinCoords /= TabletToPhysicialCoordRatio;
        MaxCoords /= TabletToPhysicialCoordRatio;
        
        CanvasRotation = OutputMode.Input.Rotation;

        return true;
    }

    private Vector2 ClampInput(Vector2 pos)
    {
        if (bDropOutOfBoundsReports && !IsWithin(pos, Vector2.Zero, MaxDigitizerCoords))
        {
            return Vector2.Zero;
        }

        if (bAbsClampOobInput && !IsWithin(pos, MinCoords, MaxCoords, CanvasRotation))
        {
            if (bAbsIgnoreOobInput)
            {
                return Vector2.Zero;
            }
            return Clamp(pos, MinCoords, MaxCoords, CanvasRotation);
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
        if (bMouseAccelerationEnabled)
        {
            accelerationMultiplier = MouseAcceleration.GetMultiplier(velocity) * (float)Math.Sqrt(AccelerationIntensity) / 8; // divide to compensate for larger pen movement compared to mouse;
        }

        return displacement * GetAspectRatioConversionVector() * accelerationMultiplier * SpeedMultiplier;
    }

    private Vector2 GetAspectRatioConversionVector()
    {
        if (AspectRatioConversionRatio != Vector2.Zero)
        {
            // return previously calculated value
            return AspectRatioConversionRatio;
        }

        // only calculated the first time OutputMode is found
        AspectRatioConversionRatio = CalculateAspectRatioConversionVector();
        return AspectRatioConversionRatio;
    }

    private Vector2 CalculateAspectRatioConversionVector()
    {
        Area outputArea = OutputMode.Output;
        Area inputArea = OutputMode.Input;
           
        float outputAspectRatio = Math.Abs(outputArea.Width / outputArea.Height);
        float inputAspectRatio = Math.Abs(inputArea.Width / inputArea.Height);
        float tabletAspectRatio = Digitizer.Width / Digitizer.Height;
        float customAspectRatio = CustomAspectRatioX / CustomAspectRatioY;
        
        float targetAspectRatio = bCustomAspectRatioEnabled ? customAspectRatio : outputAspectRatio;

        targetAspectRatio *= inputAspectRatio;
        outputAspectRatio *= tabletAspectRatio;
        
        return (targetAspectRatio > outputAspectRatio)
            ? new Vector2(1f, outputAspectRatio / targetAspectRatio)
            : new Vector2(targetAspectRatio / outputAspectRatio, 1f);
    }
}