using OpenTabletDriver.Plugin;

namespace MouseMode;

public class ToggleableProperty<T> where T : notnull
{
    private EventHandler<T>? _eventHandler;
    public event EventHandler<T>? OnValueChanged
    {
        add
        {
            _eventHandler += value;
            value?.Invoke(this, _value);
        }
        
        remove => _eventHandler -= value;
    }

    private T _defaultValue = default!;

    private T _value = default!;

    public T GetValue() => _value;

    public void SetValue(T newValue)
    {
        if (_value.Equals(newValue))
            return;
        
        Log.Write("Mouse Mode", $"{newValue}");
        
        _value = newValue;
        _eventHandler?.Invoke(this, newValue);
    }
    
    public void SetDefaultValue(T newValue)
    {
        _defaultValue = newValue;
        SetValue(newValue);
    }
    
    public void ResetValue()
    {
        SetValue(_defaultValue);
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
            var boolValue = (bool)(object)_value;
            SetValue((T)(object)!boolValue);
            
            return true;
        }
        
        // generic property toggle
        if (_value.Equals(value2))
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
        return $"{{default: {_defaultValue}, value: {_value}}}" ?? "";
    }
    
}