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
        var properties = MouseModeProperties.GetOrAddProperties(tablet);
        int key = Array.IndexOf(ValidChoices, PropertyChoice);
        int? parsedInt = null;
        bool? parsedBool = null;
        float? parsedFloat = null;
        switch (PropertyChoice)
        {
            case RESET_TIME_NAME: // int
                parsedInt = int.Parse(Value);
                break;
                
            case IGNORE_OOB_TABLET_INPUT_NAME:
            case NORMALIZE_ASPECT_RATIO_NAME:
            case ACCELERATION_ENABLED_NAME: // bool
                parsedBool = true;
                break;
                
            case SPEED_MULTIPLIER_NAME:
            case ACCELERATION_INTENSITY_NAME: // float
                parsedFloat = float.Parse(Value);
                break;
        }

        switch (ActionChoice)
        {
            case TOGGLE_ACTION:
                if(parsedInt != null)
                    properties.ToggleValue(key, (int)parsedInt);
                else if (parsedBool != null)
                    properties.ToggleValue(key, (bool)parsedBool);
                else if(parsedFloat != null)
                    properties.ToggleValue(key, (float)parsedFloat);
                break;
            
            case HOLD_ACTION:
                if(parsedInt != null)
                    properties.SetValue(key, (int)parsedInt);
                else if (parsedBool != null)
                    properties.SetValue(key, (bool)parsedBool);
                else if(parsedFloat != null)
                    properties.SetValue(key, (float)parsedFloat);
                break;
        }
    }

    public void Release(TabletReference tablet, IDeviceReport report)
    {
        if (ActionChoice == TOGGLE_ACTION) return;

        var properties = MouseModeProperties.GetOrAddProperties(tablet);
        int key = Array.IndexOf(ValidChoices, PropertyChoice);
        switch (PropertyChoice)
        {
            case RESET_TIME_NAME: // int
                properties.ResetValue<int>(key);
                break;
                
            case IGNORE_OOB_TABLET_INPUT_NAME:
            case NORMALIZE_ASPECT_RATIO_NAME:
            case ACCELERATION_ENABLED_NAME: // bool
                properties.ResetValue<bool>(key);
                break;
                
            case SPEED_MULTIPLIER_NAME:
            case ACCELERATION_INTENSITY_NAME: // float
                properties.ResetValue<float>(key);
                break;
        }
    }
}