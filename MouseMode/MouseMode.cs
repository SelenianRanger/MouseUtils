using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

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
    public bool bCustomAspectRationEnabled { get; set; }

    [Property("Custom Aspect Ratio X"), DefaultPropertyValue(16)]
    public int CustomAspectRatioX { get; set; }

    [Property("Custom Aspect Ratio Y"), DefaultPropertyValue(9)]
    public int CustomAspectRatioY { get; set; }

    [Property("Speed Multiplier"), DefaultPropertyValue(1)]
    public float SpeedMultiplier { get; set; }

    [BooleanProperty("Use Windows Mouse Acceleration Curve", ""),
     DefaultPropertyValue(true)]
    public bool bMouseAccelerationEnabled { get; set; }

    [Property("Acceleration Intensity"), DefaultPropertyValue(1)]
    public float AccelerationIntensity { get; set; }

    private TimeSpan ResetTimeSpan;
    private HPETDeltaStopwatch stopwatch = new HPETDeltaStopwatch(true);

    private Vector2 MaxCoords;
    private Vector2 CanvasOrigin = Vector2.Zero;
    private Vector2 CustomAspectRatio;
    private Vector2 TabletToPhysicialCoordRatio = Vector2.One;

    // based on values used in windows, source: https://www.esreality.com/index.php?a=post&id=1945096
    private Vector2[] AccCurve =
    {
        new Vector2(0, 0),
        new Vector2(0.43f, 1.37f),
        new Vector2(1.25f, 5.3f),
        new Vector2(3.86f, 24.3f),
        new Vector2(40, 568)
    };

    private Vector2 LastPos = Vector2.Zero;
    private Vector2 LastLocalPos = Vector2.Zero;

    [TabletReference] 
    public TabletReference TabletRef { get; set; }
    
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
        TabletToPhysicialCoordRatio /= 25.4f; // convert mm to inch to fit curve value

        if (bCustomAspectRationEnabled)
        {
            // TODO
        }
        
        CustomAspectRatio = new Vector2(CustomAspectRatioX, CustomAspectRatioY);
    }
    
    public void Consume(IDeviceReport report)
    {
        // skip if no report
        if(report is OutOfRangeReport)
            return;

        var pos = Vector2.Zero;

        if (report is ITabletReport r1)
        {
            pos = r1.Position;
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

        // calculate and scale movement velocity
        var displacement = pos - LastPos;
        float velocity = (float)((displacement * TabletToPhysicialCoordRatio).Length() / deltaTime.TotalSeconds);
        float displacementMultiplier = 1f;
        if (bMouseAccelerationEnabled)
        {
            displacementMultiplier *= GetAcceleration(velocity) / 5; // divide by 5 to compensate for larger pen movement compared to mouse
        }
        displacementMultiplier *= SpeedMultiplier;

        // apply scaled movement in transformed origin space
        var transformedPos = Vector2.Clamp( LastLocalPos + CanvasOrigin + displacement * displacementMultiplier,
            Vector2.Zero, MaxCoords);

        // save final coordinates and report
        if (report is ITabletReport r2)
        {
            r2.Position = transformedPos;
            report = r2;
        }
        
        Emit?.Invoke(report);

        LastPos = pos;
        LastLocalPos = transformedPos - CanvasOrigin;
    }

    public event Action<IDeviceReport>? Emit;
    public PipelinePosition Position => PipelinePosition.PreTransform;

    private bool IsNearly(float A, float B, float error = Single.Epsilon)
    {
        return Math.Abs(A - B) < error;
    }
    
    private float GetAcceleration(float value)
    {
        int i = 1;
        while (i < AccCurve.Length - 1 && value > AccCurve[i].X) i++;
        float interpRatio = (value - AccCurve[i - 1].X) / (AccCurve[i].X - AccCurve[i - 1].X);
        return IsNearly(value, 0f, 0.1f) ? 0 : (float)Math.Sqrt(AccelerationIntensity) * Vector2.Lerp(AccCurve[i-1], AccCurve[i], interpRatio).Y / value;
    }
}