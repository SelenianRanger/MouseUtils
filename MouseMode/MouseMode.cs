using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace MouseMode;

[PluginName("MouseMode")]
public class MouseMode : IPositionedPipelineElement<IDeviceReport>
{
    public void Consume(IDeviceReport value)
    {
        Emit?.Invoke(value);
    }

    public event Action<IDeviceReport>? Emit;
    public PipelinePosition Position => PipelinePosition.PreTransform;
}