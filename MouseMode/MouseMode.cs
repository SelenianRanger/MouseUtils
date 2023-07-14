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

    private TimeSpan ResetTimeSpan;
    private HPETDeltaStopwatch stopwatch = new HPETDeltaStopwatch(true);

    private Vector2 MaxCoords;
    private Vector2 CanvasOrigin = Vector2.Zero;
    private Vector2 AspectRatioConversionRatio = Vector2.One;
    private Vector2 TabletToPhysicialCoordRatio = Vector2.One;

    private Vector2 LastPos = Vector2.Zero;
    private Vector2 LastLocalPos = Vector2.Zero;

    [TabletReference] 
    public TabletReference TabletRef { get; set; }

    [Resolved] 
    public IDriver DriverRef { get; set; }

    private IOutputMode? OutputMode;

    [OnDependencyLoad]
    public void Recompile()
    {
        var digitizer = TabletRef.Properties.Specifications.Digitizer;

        ResetTimeSpan = new TimeSpan(0, 0, 0, 0, ResetTime);

        CanvasOrigin = Vector2.Zero;
        MaxCoords = new Vector2(digitizer.MaxX, digitizer.MaxY);

        TabletToPhysicialCoordRatio = new Vector2(
            digitizer.Width / digitizer.MaxX,
            digitizer.Height / digitizer.MaxY);
        TabletToPhysicialCoordRatio /= 25.4f; // convert mm to inch to fit curve values
    }
    
    public void Consume(IDeviceReport report)
    {
        // skip if no report
        if(report is OutOfRangeReport)
            return;

        var pos = Vector2.Zero;

        if (report is ITabletReport tabletReport)
        {
            pos = tabletReport.Position;
        }
        else
        {
            Emit?.Invoke(report);
            return;
        }
        
        // drop report if out of tablet work area
        var digitizer = TabletRef.Properties.Specifications.Digitizer;
        if(IsNearly(pos.X, 0, 0.1f) || IsNearly(pos.X, digitizer.MaxX, 0.1f) || 
           IsNearly(pos.Y, 0, 0.1f) || IsNearly(pos.Y, digitizer.MaxY, 0.1f))
            return;

        // check for reset timeout
        var deltaTime = stopwatch.Restart();
        if (ResetTime >= 0 && deltaTime >= ResetTimeSpan)
        {
            CanvasOrigin = LastLocalPos + CanvasOrigin;
            LastPos = pos;
            LastLocalPos = Vector2.Zero;
        }

        // calculate movement velocity
        var displacement = pos - LastPos;
        float velocity = (float)((displacement * TabletToPhysicialCoordRatio).Length() / deltaTime.TotalSeconds);

        // apply scaled movement in transformed origin space
        displacement *= GetAspectRatioConversionVector() * GetSpeedMultiplier(velocity);
        var transformedPos = Vector2.Clamp( LastLocalPos + CanvasOrigin + displacement,
            Vector2.Zero, MaxCoords);

        // save final coordinates and report
        tabletReport.Position = transformedPos;
        Emit?.Invoke(tabletReport);

        LastPos = pos;
        LastLocalPos = transformedPos - CanvasOrigin;
    }

    public event Action<IDeviceReport>? Emit;
    public PipelinePosition Position => PipelinePosition.PreTransform;

    private void TryGetOutputMode()
    {
        if (DriverRef is not Driver drv)
            return;
        
        OutputMode = drv.InputDevices
            .Where(device => device?.OutputMode?.Elements?.Contains(this) ?? false)
            .Select(device => device?.OutputMode).FirstOrDefault();
        
        if (OutputMode == null)
        {
            Log.Write("Mouse Mode", "Could not find a reference to current OutputMode", LogLevel.Error);
        }
    }

    private Vector2 GetAspectRatioConversionVector()
    {
        if (OutputMode == null)
        {
            TryGetOutputMode();
        }
        else
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
        var digitizer = TabletRef.Properties.Specifications.Digitizer;
        float tabletAspectRatio = digitizer.Width / digitizer.Height;
        float inputAspectRatio = tabletAspectRatio; // fallback value
        float outputAspectRatio = 16f / 9f; // fallback value
        float customAspectRatio = CustomAspectRatioX / CustomAspectRatioY;
        
        float targetAspectRatio = bCustomAspectRatioEnabled ? customAspectRatio : outputAspectRatio;

        if (OutputMode is AbsoluteOutputMode absOutputMode)
        {
            Area outputArea = absOutputMode.Output;
            Area inputArea = absOutputMode.Input;
               
            outputAspectRatio = Math.Abs(outputArea.Width / outputArea.Height);
            inputAspectRatio = Math.Abs(inputArea.Width / inputArea.Height);
        }
        else if (OutputMode is RelativeOutputMode relOutputMode)
        {
            Vector2 inputSensitivity = relOutputMode.Sensitivity;
            outputAspectRatio = 1f;
            inputAspectRatio = Math.Abs(inputSensitivity.X / inputSensitivity.Y);
        }

        targetAspectRatio *= inputAspectRatio;
        outputAspectRatio *= tabletAspectRatio;

        return (targetAspectRatio > outputAspectRatio)
            ? new Vector2(1f, outputAspectRatio / targetAspectRatio)
            : new Vector2(targetAspectRatio / outputAspectRatio, 1f);
    }

    private float GetSpeedMultiplier(float velocity)
    {
        float multiplier = 1f;
        if (bMouseAccelerationEnabled)
        {
            multiplier *= MouseAcceleration.GetMultiplier(velocity) * (float)Math.Sqrt(AccelerationIntensity) / 5; // divide to compensate for larger pen movement compared to mouse
        }
        return multiplier * SpeedMultiplier;
    }
}