namespace MouseMode;

public static class MouseModeStatics
{
    public static bool IsNearly(float A, float B, float error = Single.Epsilon)
    {
        return Math.Abs(A - B) < error;
    }
}