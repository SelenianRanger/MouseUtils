using System.Runtime.InteropServices;
using OpenTabletDriver.Plugin.Tablet;

namespace MouseMode;
using static MouseModeConstants;

public class MouseModeProperties
{

    private static readonly Dictionary<TabletReference, MouseModeProperties> TabletPropertiesMap = new(new TabletComparer());
    private readonly Dictionary<int, object> PropertyDictInstance = new();

    MouseModeProperties()
    {
        PopulatePropertyDict();
    }

    public static MouseModeProperties GetOrAddProperties(TabletReference tabletRef)
    {
        lock (TabletPropertiesMap)
        {
            ref var propertiesInstance = ref CollectionsMarshal.GetValueRefOrAddDefault(TabletPropertiesMap, tabletRef, out var exists);
            if (!exists)
            {
                propertiesInstance = new MouseModeProperties();
            }

            return propertiesInstance!;
        }
    }

    private void PopulatePropertyDict()
    {
        PropertyDictInstance.Add(RESET_TIME_INDEX, new ToggleableProperty<int>());
        PropertyDictInstance.Add(IGNORE_OOB_TABLET_INPUT_INDEX, new ToggleableProperty<bool>());
        PropertyDictInstance.Add(NORMALIZE_ASPECT_RATIO_INDEX, new ToggleableProperty<bool>());
        PropertyDictInstance.Add(SPEED_MULTIPLIER_INDEX, new ToggleableProperty<float>());
        PropertyDictInstance.Add(ACCELERATION_ENABLED_INDEX, new ToggleableProperty<bool>());
        PropertyDictInstance.Add(ACCELERATION_INTENSITY_INDEX, new ToggleableProperty<float>());
    }

    public ToggleableProperty<T>? GetProperty<T>(int key) where T : notnull
    {
        if (PropertyDictInstance.TryGetValue(key, out var value) && value is ToggleableProperty<T> property)
        {
            return property;
        }
        
        return default;
    }

    public void SetDefault<T>(int key, T newValue) where T : notnull
    {
        GetProperty<T>(key)?.SetDefaultValue(newValue);
    }
    
    public T? GetValue<T>(int key) where T : notnull
    {
        var property = GetProperty<T>(key);
        return property != null ? property.GetValue() : default;
    }

    public void SetValue<T>(int key, T newValue) where T : notnull
    {
        GetProperty<T>(key)?.SetValue(newValue);
    }

    public void ToggleValue<T>(int key, T newValue) where T : notnull
    {
        GetProperty<T>(key)?.ToggleValue(newValue);
    }

    public void ResetValue<T>(int key) where T : notnull
    {
        GetProperty<T>(key)?.ResetValue();
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