namespace MouseMode;
using static MouseModeConstants;

public static class MouseModeProperties
{
    private static readonly Dictionary<int, object> PropertyDict = new();

    static MouseModeProperties()
    {
        PropertyDict.Add(RESET_TIME_INDEX, new ToggleableProperty<int>(RESET_TIME_DEFAULT));
        PropertyDict.Add(IGNORE_OOB_TABLET_INPUT_INDEX, new ToggleableProperty<bool>(IGNORE_OOB_TABLET_INPUT_DEFAULT));
        PropertyDict.Add(NORMALIZE_ASPECT_RATIO_INDEX, new ToggleableProperty<bool>(NORMALIZE_ASPECT_RATIO_DEFAULT));
        PropertyDict.Add(SPEED_MULTIPLIER_INDEX, new ToggleableProperty<float>(SPEED_MULTIPLIER_DEFAULT));
        PropertyDict.Add(ACCELERATION_ENABLED_INDEX, new ToggleableProperty<bool>(ACCELERATION_ENABLED_DEFAULT));
        PropertyDict.Add(ACCELERATION_INTENSITY_INDEX, new ToggleableProperty<float>(ACCELERATION_INTENSITY_DEFAULT));
    }

    public static ToggleableProperty<T>? GetProperty<T>(int key) where T : notnull
    {
        if (PropertyDict.TryGetValue(key, out var value) && value is ToggleableProperty<T> property)
        {
            return property;
        }

        return default;
    }

    public static void SetDefault<T>(int key, T newValue) where T : notnull
    {
        GetProperty<T>(key)?.SetDefaultValue(newValue);
    }
    
    public static T? GetValue<T>(int key) where T : notnull
    {
        var property = GetProperty<T>(key);
        return property != null ? property.GetValue() : default;
    }

    public static void SetValue<T>(int key, T newValue) where T : notnull
    {
        GetProperty<T>(key)?.SetValue(newValue);
    }

    public static void ToggleValue<T>(int key, T newValue) where T : notnull
    {
        GetProperty<T>(key)?.ToggleValue(newValue);
    }

    public static void ResetValue<T>(int key) where T : notnull
    {
        GetProperty<T>(key)?.ResetValue();
    }
}