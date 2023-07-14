using System.Numerics;

namespace MouseMode;

public static class MouseModeStatics
{
    /// <returns>Whether floats <c>A</c> and <c>B</c> are almost equal with the set degree of <c>error</c></returns>
    public static bool IsNearly(float A, float B, float error = 0.0001f)
    {
        return Math.Abs(A - B) < error;
    }
    
    
    /// <returns>Whether point <c>A</c> falls withing the boundary of the rectangle defined by corners <c>X</c> and <c>Y</c></returns>
    public static bool IsWithin(Vector2 A, Vector2 X, Vector2 Y)
    {
        return  X.X < A.X && A.X < Y.X &&
                X.Y < A.Y && A.Y < Y.Y;
    }
}