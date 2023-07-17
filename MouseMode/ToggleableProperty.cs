using System.Diagnostics.CodeAnalysis;

namespace MouseMode;

public class ToggleableProperty<T> where T : notnull
{
    public delegate void ValueChangedHandler(T newValue);

    public event ValueChangedHandler? OnValueChanged;

    private T DefaultValue = default!;

    private T Value = default!;
    
    public ToggleableProperty([DisallowNull]T defaultValue)
    {
        SetDefaultValue(defaultValue);
    }

    public T GetValue() => Value;

    public void SetValue(T newValue)
    {
        if (Value.Equals(newValue))
            return;
        
        Value = newValue;
        OnValueChanged?.Invoke(newValue);
    }
    
    public void SetDefaultValue(T newValue)
    {
        DefaultValue = newValue;
        SetValue(newValue);
    }
    
    public void ResetValue()
    {
        SetValue(DefaultValue);
    }

    /// <summary>
    /// tried to toggle the property value as a bool, if not possible toggles the value between value2, and default value
    /// </summary>
    /// <returns>whether toggling as a bool succeeded or not</returns>
    public bool ToggleValue(T value2)
    {
        // bool property toggle
        if (typeof(T) == typeof(bool))
        {
            var boolValue = (bool)(object)Value;
            SetValue((T)(object)!boolValue);
            
            return true;
        }
        
        // generic property toggle
        if (Value.Equals(value2))
        {
            ResetValue();
        }
        else
        {
            SetValue(value2);
        }

        return false;
    }

    public override string ToString()
    {
        return Value.ToString() ?? "";
    }
    
}