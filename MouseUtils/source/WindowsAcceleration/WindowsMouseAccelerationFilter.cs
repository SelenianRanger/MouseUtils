using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;
using static MouseUtils.Constants;
using static MouseUtils.Extensions;

namespace MouseUtils.WindowsAcceleration;

[PluginName("Windows Mouse Acceleration")]
public class WindowsMouseAccelerationFilter : FilterBase
{
    [Property(ACCELERATION_INTENSITY_NAME),
     DefaultPropertyValue(ACCELERATION_INTENSITY_DEFAULT)]
    public float AccelerationIntensity { get; set; }
    
    // internal variables
    private float _accelerationIntensity;
    
    private readonly HPETDeltaStopwatch _stopwatch = new HPETDeltaStopwatch(true);

    private Matrix3x2 _outputTransfrom;
    private Matrix3x2 _invOutputTransfrom;
    private Vector2 _tabletToPhysicalCoords;

    public override PipelinePosition Position => PipelinePosition.PostTransform;

    protected override void OnValidReport(IDeviceReport report)
    {
        var tabletReport = report as ITabletReport;
        var displacement = tabletReport!.Position;
        
        var deltaTime = _stopwatch.Restart();
        tabletReport.Position = ScaleDisplacement(displacement, (float)deltaTime.TotalSeconds);

        InvokeEmit(tabletReport);
    }

    protected override bool IsValidReport(IDeviceReport report)
    {
        if (!base.IsValidReport(report))
            return false;

        if (OutputMode is not RelativeOutputMode)
            return false;
        
        if (report is not ITabletReport)
            return false;
        
        return true;
    }

    protected override void SubscribeToPropertyChanges()
    {
        _properties.GetProperty<float>(PropertyIndex.AccelerationIntensity)!.OnValueChanged += (_, value) => _accelerationIntensity = value;
    }

    protected override void InitializeProperties()
    {
        _properties.SetDefault(PropertyIndex.AccelerationIntensity, AccelerationIntensity);
    }

    protected override void OnOutputModeFound()
    {
        if (OutputMode is not RelativeOutputMode relOutputMode)
            return;

        _outputTransfrom = relOutputMode.TransformationMatrix;
        Matrix3x2.Invert(_outputTransfrom, out _invOutputTransfrom);
        _tabletToPhysicalCoords = GetTabletToPhysicalRatio(TabletRef);
    }
    
    private Vector2 ScaleDisplacement(Vector2 outputDisplacement, float deltaTime)
    {
        var inputDisplacement = Vector2.Transform(outputDisplacement, _invOutputTransfrom); // transform to input space
        float velocity = (inputDisplacement * _tabletToPhysicalCoords / 25.4f).Length() / deltaTime; // convert mm/s to inch/s
        float accelerationMultiplier = 1f;
        if (_accelerationIntensity != 0)
        {
            accelerationMultiplier = WindowsMouseAcceleration.GetMultiplier(velocity) * (float)Math.Sqrt(_accelerationIntensity);
        }
        
        return  Vector2.Transform(inputDisplacement * accelerationMultiplier, _outputTransfrom);
    }

    protected override void OnDispose()
    {
        _properties.SetDefault(PropertyIndex.AccelerationIntensity, 0f); // disable acceleration
        base.OnDispose();
    }
}