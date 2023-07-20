using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;

namespace MouseUtils.WindowsAcceleration;

[PluginName("Windows Mouse Acceleration")]
public class WindowsMouseAccelerationBinding : BindingBase
{
    // referenced by driver
    public new static string[] ValidActions => BindingBase.ValidActions;

    protected override float OnToggle(IDeviceReport report, out Constants.PropertyIndex updatedPropertyIndex)
    {
        updatedPropertyIndex = Constants.PropertyIndex.AccelerationIntensity;
        return PropertiesInst.ToggleValue(updatedPropertyIndex, ToggleValue);
    }

    protected override float OnHold(IDeviceReport report, out Constants.PropertyIndex updatedPropertyIndex)
    {
        updatedPropertyIndex = Constants.PropertyIndex.AccelerationIntensity;
        return PropertiesInst.SetValue(updatedPropertyIndex, ToggleValue);
    }

    protected override float OnRelease(IDeviceReport report, out Constants.PropertyIndex updatedPropertyIndex)
    {
        updatedPropertyIndex = Constants.PropertyIndex.AccelerationIntensity;
        return PropertiesInst.ResetValue<float>(updatedPropertyIndex);
    }
}