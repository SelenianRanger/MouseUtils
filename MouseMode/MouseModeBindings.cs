using System.Globalization;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using static MouseMode.MouseModeConstants;
using static MouseMode.MouseModeExtensions;

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
    
    [Property("Value"), DefaultPropertyValue(1f)
     , ToolTip("value set to properties that are not checkboxes")]
    public float Value { get; set; }
    
    [BooleanProperty("Show Notification When Values Change", ""), DefaultPropertyValue(false)]
    public bool bShowNotif { get; set; }
    
    public void Press(TabletReference tablet, IDeviceReport report)
    {
        var properties = MouseModeProperties.GetOrAddProperties(tablet, out _);
        var key = (PropertyIndex)Array.IndexOf(ValidChoices, PropertyChoice);
        int? parsedInt = null;
        bool? parsedBool = null;
        float? parsedFloat = null;
        switch (PropertyChoice)
        {
            case RESET_TIME_NAME: // int
                parsedInt = (int)Value;
                break;
                
            case IGNORE_OOB_TABLET_INPUT_NAME:
            case NORMALIZE_ASPECT_RATIO_NAME:
            case ACCELERATION_ENABLED_NAME: // bool
                parsedBool = true;
                break;
                
            case SPEED_MULTIPLIER_NAME:
            case ACCELERATION_INTENSITY_NAME: // float
                parsedFloat = Value;
                break;
        }

        float newValue = 0f;
        if (parsedBool != null)
        {
            // toggle bool property regardless of action type
            newValue = properties.ToggleValue(key, (bool)parsedBool) ? 1f : 0f;
        }
        else
        {
            switch (ActionChoice)
            {
                case TOGGLE_ACTION:
                    if(parsedInt != null)
                        newValue = properties.ToggleValue(key, (int)parsedInt);
                    else if(parsedFloat != null)
                        newValue = properties.ToggleValue(key, (float)parsedFloat);
                    break;
            
                case HOLD_ACTION:
                    if(parsedInt != null)
                        newValue = properties.SetValue(key, (int)parsedInt);
                    else if(parsedFloat != null)
                        newValue = properties.SetValue(key, (float)parsedFloat);
                    break;
            }
        }

        if (bShowNotif)
        {
            ShowNotif(newValue);
        }
        
    }

    public void Release(TabletReference tablet, IDeviceReport report)
    {
        if (ActionChoice == TOGGLE_ACTION) return;

        var properties = MouseModeProperties.GetOrAddProperties(tablet, out _);
        var key = (PropertyIndex)Array.IndexOf(ValidChoices, PropertyChoice);
        float newValue = 0f;
        switch (PropertyChoice)
        {
            case RESET_TIME_NAME: // int
                newValue = properties.ResetValue<int>(key);
                break;
                
            case IGNORE_OOB_TABLET_INPUT_NAME:
            case NORMALIZE_ASPECT_RATIO_NAME:
            case ACCELERATION_ENABLED_NAME: // bool
                newValue = properties.ToggleValue(key, true) ? 1f : 0f;
                break;
                
            case SPEED_MULTIPLIER_NAME:
            case ACCELERATION_INTENSITY_NAME: // float
                newValue = properties.ResetValue<float>(key);
                break;
        }

        if (bShowNotif)
        {
            ShowNotif(newValue, true);
        }
    }

    private void ShowNotif(float value, bool reset = false)
    {
        string valueStr = "N/A";
        switch (PropertyChoice)
        {
            case RESET_TIME_NAME: // int
                valueStr = ((int)value).ToString();
                break;
                
            case IGNORE_OOB_TABLET_INPUT_NAME:
            case NORMALIZE_ASPECT_RATIO_NAME:
            case ACCELERATION_ENABLED_NAME: // bool
                valueStr = IsNearly(value, 0f) ? "Disabled" : "Enabled";
                break;
                
            case SPEED_MULTIPLIER_NAME:
            case ACCELERATION_INTENSITY_NAME: // float
                valueStr = value.ToString("0.00", CultureInfo.InvariantCulture);
                break;
        }

        var message = $"{PropertyChoice} was {(reset ? "reset" : "set")} to {valueStr}";
        Log.WriteNotify("Mouse Mode", message);
    }
}