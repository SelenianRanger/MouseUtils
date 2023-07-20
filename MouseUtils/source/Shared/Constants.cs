namespace MouseUtils;

public static class Constants
{
    public const string RESET_TIME_NAME = "Reset Time";
    public const int RESET_TIME_DEFAULT = 100;
    
    public const string IGNORE_OOB_TABLET_INPUT_NAME = "Ignore Input Outside Physical Tablet Area";
    public const bool IGNORE_OOB_TABLET_INPUT_DEFAULT = true;
    
    public const string NORMALIZE_ASPECT_RATIO_NAME = "Normalize Aspect Ratio";
    public const bool NORMALIZE_ASPECT_RATIO_DEFAULT = true;
    
    public const string SPEED_MULTIPLIER_NAME = "Speed Multiplier";
    public const float SPEED_MULTIPLIER_DEFAULT = 1f;

    public const string ACCELERATION_INTENSITY_NAME = "Windows Mouse Acceleration Intensity";
    public const float ACCELERATION_INTENSITY_DEFAULT = 1f;
    
    public enum PropertyIndex
    {
        ResetTime = 0,
        IgnoreOobTabletInput,
        NormalizeAspectRatio,
        SpeedMultiplier,
        AccelerationIntensity
    }

    public static readonly string[] PropertyNames =
    {
        RESET_TIME_NAME,
        IGNORE_OOB_TABLET_INPUT_NAME,
        NORMALIZE_ASPECT_RATIO_NAME,
        SPEED_MULTIPLIER_NAME,
        ACCELERATION_INTENSITY_NAME
    };
    
    public static readonly string[] Abs2RelPropertyNames =
    {
        RESET_TIME_NAME,
        IGNORE_OOB_TABLET_INPUT_NAME,
        NORMALIZE_ASPECT_RATIO_NAME,
        SPEED_MULTIPLIER_NAME
    };
}