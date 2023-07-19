using System.Runtime.InteropServices;
using OpenTabletDriver.Plugin.Tablet;

namespace MouseMode;
using static MouseModeConstants;

public class MouseModeProperties
{

    private static readonly Dictionary<TabletReference, MouseModeProperties> TabletPropertiesMap = new(new TabletComparer());
    private readonly Dictionary<PropertyIndex, object> _propertyDictInstance = new();

    MouseModeProperties()
    {
        PopulatePropertyDict();
    }

    public static MouseModeProperties GetOrAddProperties(TabletReference tabletRef, out bool found)
    {
        found = true;
        lock (TabletPropertiesMap)
        {
            ref var propertiesInstance = ref CollectionsMarshal.GetValueRefOrAddDefault(TabletPropertiesMap, tabletRef, out var exists);
            if (!exists)
            {
                propertiesInstance = new MouseModeProperties();
                found = false;
            }

            return propertiesInstance!;
        }
    }

    private void PopulatePropertyDict()
    {
        _propertyDictInstance.Add(PropertyIndex.ResetTime, new ToggleableProperty<int>());
        _propertyDictInstance.Add(PropertyIndex.IgnoreOobTabletInput, new ToggleableProperty<bool>());
        _propertyDictInstance.Add(PropertyIndex.NormalizeAspectRatio, new ToggleableProperty<bool>());
        _propertyDictInstance.Add(PropertyIndex.SpeedMultiplier, new ToggleableProperty<float>());
        _propertyDictInstance.Add(PropertyIndex.AccelerationEnabled, new ToggleableProperty<bool>());
        _propertyDictInstance.Add(PropertyIndex.AccelerationIntensity, new ToggleableProperty<float>());
    }

    public ToggleableProperty<T>? GetProperty<T>(PropertyIndex key) where T : notnull
    {
        if (_propertyDictInstance.TryGetValue(key, out var value) && value is ToggleableProperty<T> property)
        {
            return property;
        }
        
        return default;
    }

    public T? SetDefault<T>(PropertyIndex key, T newValue) where T : notnull
    {
        var property = GetProperty<T>(key);
        return property != null ? property.SetDefaultValue(newValue) : default;
    }
    
    public T? GetValue<T>(PropertyIndex key) where T : notnull
    {
        var property = GetProperty<T>(key);
        return property != null ? property.GetValue() : default;
    }

    public T? SetValue<T>(PropertyIndex key, T newValue) where T : notnull
    {
        var property = GetProperty<T>(key);
        return property != null ? property.SetValue(newValue) : default;
    }

    public T? ToggleValue<T>(PropertyIndex key, T newValue) where T : notnull
    {
        var property = GetProperty<T>(key);
        return property != null ? property.ToggleValue(newValue) : default;
    }

    public T? ResetValue<T>(PropertyIndex key) where T : notnull
    {
        var property = GetProperty<T>(key);
        return property != null ? property.ResetValue() : default;
    }

    public void UnsubscribeFromAll(object target)
    {
        UnsubscribeFromProperty(PropertyIndex.ResetTime, target);
        UnsubscribeFromProperty(PropertyIndex.IgnoreOobTabletInput, target);
        UnsubscribeFromProperty(PropertyIndex.NormalizeAspectRatio, target);
        UnsubscribeFromProperty(PropertyIndex.SpeedMultiplier, target);
        UnsubscribeFromProperty(PropertyIndex.AccelerationEnabled, target);
        UnsubscribeFromProperty(PropertyIndex.AccelerationIntensity, target);
    }

    public void UnsubscribeFromProperty(PropertyIndex key, object target)
    {
        switch (key)
        {
            case PropertyIndex.ResetTime: // int
                (_propertyDictInstance[key] as ToggleableProperty<int>)?.UnsubscribeObject(target);
                break;
            
            case PropertyIndex.IgnoreOobTabletInput:
            case PropertyIndex.NormalizeAspectRatio:
            case PropertyIndex.AccelerationEnabled: // bool
                (_propertyDictInstance[key] as ToggleableProperty<bool>)?.UnsubscribeObject(target);
                break;
            
            case PropertyIndex.SpeedMultiplier:
            case PropertyIndex.AccelerationIntensity: // float
                (_propertyDictInstance[key] as ToggleableProperty<float>)?.UnsubscribeObject(target);
                break;
        }
    }
    
    private class TabletComparer : IEqualityComparer<TabletReference>
    {
        public bool Equals(TabletReference? a, TabletReference? b)
        {
            return a?.Properties.Name == b?.Properties.Name;
        }

        public int GetHashCode(TabletReference obj)
        {
            return obj.Properties.Name.GetHashCode();
        }
    }
}