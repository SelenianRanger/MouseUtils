namespace MouseMode;

public static class MouseModeConstants
{
    public const int RESET_TIME_INDEX = 0;
    public const string RESET_TIME_NAME = "Reset Time";
    public const int RESET_TIME_DEFAULT = 100;

    public const int IGNORE_OOB_TABLET_INPUT_INDEX = 1;
    public const string IGNORE_OOB_TABLET_INPUT_NAME = "Ignore Input Outside Full Tablet Area";
    public const bool IGNORE_OOB_TABLET_INPUT_DEFAULT = true;

    public const int NORMALIZE_ASPECT_RATIO_INDEX = 2;
    public const string NORMALIZE_ASPECT_RATIO_NAME = "Normalize Aspect Ratio";
    public const bool NORMALIZE_ASPECT_RATIO_DEFAULT = true;

    public const int SPEED_MULTIPLIER_INDEX = 3;
    public const string SPEED_MULTIPLIER_NAME = "Speed Multiplier";
    public const float SPEED_MULTIPLIER_DEFAULT = 1f;

    public const int ACCELERATION_ENABLED_INDEX = 4;
    public const string ACCELERATION_ENABLED_NAME = "Use Windows Mouse Acceleration Curve";
    public const bool ACCELERATION_ENABLED_DEFAULT = true;

    public const int ACCELERATION_INTENSITY_INDEX = 5;
    public const string ACCELERATION_INTENSITY_NAME = "Acceleration Intensity";
    public const float ACCELERATION_INTENSITY_DEFAULT = 1f;
    
    public static readonly string[] PropertyNames =
    {
        RESET_TIME_NAME,
        IGNORE_OOB_TABLET_INPUT_NAME,
        NORMALIZE_ASPECT_RATIO_NAME,
        SPEED_MULTIPLIER_NAME,
        ACCELERATION_ENABLED_NAME,
        ACCELERATION_INTENSITY_NAME
    };
}