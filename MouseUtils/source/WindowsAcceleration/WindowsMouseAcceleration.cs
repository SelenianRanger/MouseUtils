using System.Numerics;
using static MouseUtils.Extensions;

namespace MouseUtils.WindowsAcceleration;

public static class WindowsMouseAcceleration
{
    // based on values used in windows - source: https://www.esreality.com/index.php?a=post&id=1945096
    private static readonly Vector2[] AccCurve = 
    {
        new(0, 0),
        new(0.43f, 1.37f),
        new(1.25f, 5.3f),
        new(3.86f, 24.3f),
        new(40, 568)
    };
    
    /// <returns>Velocity value of the acceleration curve based on input</returns>
    public static float GetValue(float input)
    {
        int i = 1;
        while (i < AccCurve.Length - 1 && input > AccCurve[i].X) i++;
        float interpRatio = (input - AccCurve[i - 1].X) / (AccCurve[i].X - AccCurve[i - 1].X);
        return Vector2.Lerp(AccCurve[i-1], AccCurve[i], interpRatio).Y / 8f; // divide to compensate for larger pen movement compared to mouse
    }
    
    /// <returns>Multiplier applied to velocity based on input</returns>
    public static float GetMultiplier(float input)
    {
        return IsNearly(input, 0f, 0.1f) ? 0 : GetValue(input) / input;
    }
    
}