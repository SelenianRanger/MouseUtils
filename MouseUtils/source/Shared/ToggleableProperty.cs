namespace MouseUtils;

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

    public T SetValue(T newValue)
    {
        if (_value.Equals(newValue))
            return _value;

        _value = newValue;
        _eventHandler?.Invoke(this, newValue);

        return _value;
    }
    
    public T SetDefaultValue(T newValue)
    {
        _defaultValue = newValue;
        return SetValue(newValue);
    }
    
    public T ResetValue()
    {
        return SetValue(_defaultValue);
    }

    /// <summary>
    /// tried to toggle the property value as a bool, if not possible toggles the value between value2, and default value
    /// </summary>
    public T ToggleValue(T value2)
    {
        // bool property toggle
        if (typeof(T) == typeof(bool))
        {
            var boolValue = (bool)(object)_value;
            return SetValue((T)(object)!boolValue);
        }

        // generic property toggle
        return _value.Equals(value2) ? ResetValue() : SetValue(value2);
    }

    public void UnsubscribeObject(object target)
    {
        if (_eventHandler?.GetInvocationList() == null)
            return;
        
        foreach (var delegateInst in _eventHandler?.GetInvocationList()!)
        {
            if (delegateInst.Target != target) continue;
            _eventHandler -= (EventHandler<T>)delegateInst;
        }
    }

    public override string ToString()
    {
        return $"{{default: {_defaultValue}, value: {_value}}}" ?? "";
    }
    
}