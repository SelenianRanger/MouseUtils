using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using static MouseUtils.Constants;

namespace MouseUtils.Abs2Rel;

[PluginName("Abs2Rel")]
public class Abs2RelBinding : BindingBase
{
    // referenced by driver
    public new static string[] ValidActions => BindingBase.ValidActions;

    public static string[] ValidChoices => Abs2RelPropertyNames;

    [Property("Property"), PropertyValidated(nameof(ValidChoices)), DefaultPropertyValue(RESET_TIME_NAME)]
    public string PropertyChoice { get; set; }

    protected override float OnToggle(IDeviceReport deviceReport, out PropertyIndex propertyIndex)
    {
        ParseInputValue(out var parsedInt, out var parsedBool, out var parsedFloat);
        propertyIndex = (PropertyIndex)Array.IndexOf(PropertyNames, PropertyChoice);
        if (parsedBool != null)
            return PropertiesInst.ToggleValue(propertyIndex, (bool)parsedBool) ? 1f : 0f;
        
        if(parsedInt != null)
            return PropertiesInst.ToggleValue(propertyIndex, (int)parsedInt);
        
        if(parsedFloat != null)
            return PropertiesInst.ToggleValue(propertyIndex, (float)parsedFloat);
        
        return default;
    }

    protected override float OnHold(IDeviceReport deviceReport, out PropertyIndex propertyIndex)
    {
        ParseInputValue(out var parsedInt, out var parsedBool, out var parsedFloat);
        propertyIndex = (PropertyIndex)Array.IndexOf(PropertyNames, PropertyChoice);
        if (parsedBool != null)
            return PropertiesInst.ToggleValue(propertyIndex, (bool)parsedBool) ? 1f : 0f;
        
        if(parsedInt != null)
            return PropertiesInst.SetValue(propertyIndex, (int)parsedInt);
        
        if(parsedFloat != null)
            return PropertiesInst.SetValue(propertyIndex, (float)parsedFloat);

        return default;
    }

    protected override float OnRelease(IDeviceReport deviceReport, out PropertyIndex propertyIndex)
    {
        propertyIndex = (PropertyIndex)Array.IndexOf(PropertyNames, PropertyChoice);
        var newValue = 0f;
        switch (PropertyChoice)
        {
            // int
            case RESET_TIME_NAME:
                propertyIndex = PropertyIndex.ResetTime;
                newValue = PropertiesInst.ResetValue<int>(propertyIndex);
                break;
             
            // bool
            case IGNORE_OOB_TABLET_INPUT_NAME:
                propertyIndex = PropertyIndex.IgnoreOobTabletInput;
                newValue = PropertiesInst.ToggleValue(propertyIndex, true) ? 1f : 0f;
                break;
            case NORMALIZE_ASPECT_RATIO_NAME: 
                propertyIndex = PropertyIndex.NormalizeAspectRatio;
                newValue = PropertiesInst.ToggleValue(propertyIndex, true) ? 1f : 0f;
                break;
                
            // float
            case SPEED_MULTIPLIER_NAME:
                propertyIndex = PropertyIndex.SpeedMultiplier;
                newValue = PropertiesInst.ResetValue<float>(propertyIndex);
                break;
        }

        return newValue;
    }

    private void ParseInputValue(out int? parsedInt, out bool? parsedBool, out float? parsedFloat)
    {
        var propertyIndex = (PropertyIndex)Array.IndexOf(PropertyNames, PropertyChoice);
        parsedInt = null;
        parsedBool = null;
        parsedFloat = null;
        switch (propertyIndex)
        {
            case PropertyIndex.ResetTime: // int
                parsedInt = (int)ToggleValue;
                break;
                
            case PropertyIndex.IgnoreOobTabletInput:
            case PropertyIndex.NormalizeAspectRatio: // bool
                parsedBool = true;
                break;
                
            case PropertyIndex.SpeedMultiplier:
            case PropertyIndex.AccelerationIntensity: // float
                parsedFloat = ToggleValue;
                break;
        }
    }
}