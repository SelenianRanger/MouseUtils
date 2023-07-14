using System.Numerics;
using static MouseMode.MouseModeStatics;

namespace MouseMode;

public static class MouseAcceleration
{
    // based on values used in windows - source: https://www.esreality.com/index.php?a=post&id=1945096
    private static Vector2[] AccCurve = 
    {
        new Vector2(0, 0),
        new Vector2(0.43f, 1.37f),
        new Vector2(1.25f, 5.3f),
        new Vector2(3.86f, 24.3f),
        new Vector2(40, 568)
    };
    
    public static float GetValue(float input)
    {
        int i = 1;
        while (i < AccCurve.Length - 1 && input > AccCurve[i].X) i++;
        float interpRatio = (input - AccCurve[i - 1].X) / (AccCurve[i].X - AccCurve[i - 1].X);
        return Vector2.Lerp(AccCurve[i-1], AccCurve[i], interpRatio).Y;
    }

    public static float GetMultiplier(float input)
    {
        return IsNearly(input, 0f, 0.1f) ? 0 : GetValue(input) / input;
    }
    
}