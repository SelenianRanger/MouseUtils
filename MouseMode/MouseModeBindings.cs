using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using static MouseMode.MouseModeConstants;

namespace MouseMode;

[PluginName("Mouse Mode")]
public class MouseModeBindings : IStateBinding
{
    private const string TOGGLE_ACTION = "Toggle";
    private const string HOLD_ACTION = "Hold";
    
    public static string[] ValidActions { get; } =
    {
        TOGGLE_ACTION, 
        HOLD_ACTION
    };

    public static string[] ValidChoices => PropertyNames;

    [Property("Action"), PropertyValidated(nameof(ValidActions)), DefaultPropertyValue(TOGGLE_ACTION)]
    public string ActionChoice { get; set; }
    
    [Property("Property"), PropertyValidated(nameof(ValidChoices)), DefaultPropertyValue(RESET_TIME_NAME)]
    public string PropertyChoice { get; set; }
    
    [Property("Value"), ToolTip("value set to properties that are not checkboxes")]
    public string Value { get; set; }
    
    public void Press(TabletReference tablet, IDeviceReport report)
    {
        int key = Array.IndexOf(ValidChoices, PropertyChoice);
        dynamic parsedValue;
        switch (PropertyChoice)
        {
            case RESET_TIME_NAME: // int
                parsedValue = int.Parse(Value);
                break;
                
            case IGNORE_OOB_TABLET_INPUT_NAME:
            case NORMALIZE_ASPECT_RATIO_NAME:
            case ACCELERATION_ENABLED_NAME: // bool
                parsedValue = true;
                break;
                
            case SPEED_MULTIPLIER_NAME:
            case ACCELERATION_INTENSITY_NAME: // float
                parsedValue = float.Parse(Value);
                break;
            
            default:
                parsedValue = 0;
                break;
        }

        switch (ActionChoice)
        {
            case TOGGLE_ACTION:
                MouseModeProperties.ToggleValue(key, parsedValue);
                break;
            
            case HOLD_ACTION:
                MouseModeProperties.SetValue(key, parsedValue);
                break;
        }
    }

    public void Release(TabletReference tablet, IDeviceReport report)
    {
        if (ActionChoice == TOGGLE_ACTION) return;

        int key = Array.IndexOf(ValidChoices, PropertyChoice);
        switch (PropertyChoice)
        {
            case RESET_TIME_NAME: // int
                MouseModeProperties.ResetValue<int>(key);
                break;
                
            case IGNORE_OOB_TABLET_INPUT_NAME:
            case NORMALIZE_ASPECT_RATIO_NAME:
            case ACCELERATION_ENABLED_NAME: // bool
                MouseModeProperties.ResetValue<bool>(key);
                break;
                
            case SPEED_MULTIPLIER_NAME:
            case ACCELERATION_INTENSITY_NAME: // float
                MouseModeProperties.ResetValue<float>(key);
                break;
        }
    }
}