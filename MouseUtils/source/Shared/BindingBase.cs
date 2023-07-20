using System.Globalization;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using static MouseUtils.Constants;
using static MouseUtils.Extensions;

namespace MouseUtils;

public abstract class BindingBase : IStateBinding
{
    public const string TOGGLE_ACTION = "Toggle";
    public const string HOLD_ACTION = "Hold";
    
    public static string[] ValidActions { get; } =
    {
        TOGGLE_ACTION, 
        HOLD_ACTION
    };
    
    [Property("Action"), PropertyValidated(nameof(ValidActions)), DefaultPropertyValue(TOGGLE_ACTION)]
    public string ActionChoice { get; set; }
    
    [Property("Value"), DefaultPropertyValue(1f)]
    public float ToggleValue { get; set; }
    
    [BooleanProperty("Show Notification When Values Change", ""), DefaultPropertyValue(false)]
    public bool bShowNotif { get; set; }

    private TabletReference _tabletRef;
    
    [TabletReference]
    public TabletReference TabletRef
    {
        get => _tabletRef;
        set
        {
            _tabletRef = value;
            PropertiesInst = Properties.GetOrAddProperties(value, out _);
        } 
    }

    protected Properties? PropertiesInst;
    
    public void Press(TabletReference tablet, IDeviceReport report)
    {
        PropertiesInst ??= Properties.GetOrAddProperties(tablet, out _);

        var propertyIndex = PropertyIndex.ResetTime;
        var newValue = ActionChoice switch
        {
            TOGGLE_ACTION => OnToggle(report, out propertyIndex),
            HOLD_ACTION => OnHold(report, out propertyIndex),
            _ => 0f
        };
        
        LogVariableChange(propertyIndex, newValue);
    }

    public void Release(TabletReference tablet, IDeviceReport report)
    {
        PropertiesInst ??= Properties.GetOrAddProperties(tablet, out _);

        if(ActionChoice == TOGGLE_ACTION)
            return;
        
        var newValue = OnRelease(report, out var propertyIndex);
        
        LogVariableChange(propertyIndex, newValue, true);
    }

    protected abstract float OnToggle(IDeviceReport report, out PropertyIndex updatedPropertyIndex);
    protected abstract float OnHold(IDeviceReport report, out PropertyIndex updatedPropertyIndex);
    protected abstract float OnRelease(IDeviceReport report, out PropertyIndex updatedPropertyIndex);
    
    private void LogVariableChange(PropertyIndex propertyIndex, float newValue, bool reset = false)
    {
        string valueStr;
        switch (propertyIndex)
        {
            case PropertyIndex.ResetTime: // int
                valueStr = ((int)newValue).ToString();
                break;
                
            case PropertyIndex.IgnoreOobTabletInput:
            case PropertyIndex.NormalizeAspectRatio: // bool
                valueStr = IsNearly(newValue, 0f) ? "Disabled" : "Enabled";
                break;
                
            case PropertyIndex.SpeedMultiplier:
            case PropertyIndex.AccelerationIntensity: // float
                valueStr = newValue.ToString("0.00", CultureInfo.InvariantCulture);
                break;

            default:
                valueStr = "N/A";
                break;
        }

        var message = $"{PropertyNames[(int)propertyIndex]} was {(reset ? "reset" : "set")} to {valueStr}";
        Log.Write("Mouse Utils", message, notify:bShowNotif);
        
    }
}