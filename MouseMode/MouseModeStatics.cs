using System.Numerics;
using OpenTabletDriver.Plugin;

namespace MouseMode;

public static class MouseModeStatics
{
    /// <returns>Whether floats A and B are almost equal with the set degree of error</returns>
    public static bool IsNearly(float A, float B, float error = 0.0001f)
    {
        return Math.Abs(A - B) < error;
    }
    
    
    /// <returns>Whether point P falls withing the boundary of the rectangle defined by corners X and Y, rotated by rotation</returns>
    public static bool IsWithin(Vector2 P, Vector2 X, Vector2 Y, float rotation = 0f)
    {
        Vector2 pivot = (X + Y) / 2;
        Vector2 XY = Y - X;

        if (rotation % 90 == 0)
        {
            Vector2 X2 = (rotation % 180 == 0) ? X : pivot - Flip(XY) / 2;
            Vector2 Y2 = (rotation % 180 == 0) ? Y : pivot + Flip(XY) / 2;
            return X2.X < P.X && P.X < Y2.X &&
                   X2.Y < P.Y && P.Y < Y2.Y;
        }

        float rot = rotation * (float)Math.PI / 180f;
        
        Vector2 A = X + new Vector2(XY.X, 0f);
        Vector2 B = X + new Vector2(0f, XY.Y);
        Matrix3x2 rotMatrix = Matrix3x2.CreateRotation(rot, pivot);

        Vector2 Xrot = Vector2.Transform(X, rotMatrix);
        Vector2 Arot = Vector2.Transform(A, rotMatrix);
        Vector2 Brot = Vector2.Transform(B, rotMatrix);

        Vector2 XArot = Arot - Xrot;
        Vector2 XBrot = Brot - Xrot;
        Vector2 XProt = P - Xrot;

        return  0f < Vector2.Dot(XArot, XProt) && Vector2.Dot(XArot, XProt) < Vector2.Dot(XArot, XArot) &&
                0f < Vector2.Dot(XBrot, XProt) && Vector2.Dot(XBrot, XProt) < Vector2.Dot(XBrot, XBrot);
    }

    /// <summary>
    /// Clamps point P within the rectangle defined by corners X and Y, rotated around its center
    /// </summary>
    public static Vector2 Clamp(Vector2 P, Vector2 X, Vector2 Y, float rotation = 0f)
    {
        Vector2 pivot = (X + Y) / 2;
        Vector2 XY = Y - X;

        if (rotation % 180 == 0)
        {
            return Vector2.Clamp(P, X, Y);
        }
        else if (rotation % 90 == 0)
        {
            return Vector2.Clamp(P, pivot - Flip(XY) / 2, pivot + Flip(XY) / 2);
        }
        
        float rot = rotation * (float)Math.PI / 180f;
        
        Vector2 A = X + new Vector2(XY.X, 0f);
        Vector2 B = X + new Vector2(0f, XY.Y);
        Matrix3x2 rotMatrix = Matrix3x2.CreateRotation(rot, pivot);

        Vector2 Xrot = Vector2.Transform(X, rotMatrix);
        Vector2 Arot = Vector2.Transform(A, rotMatrix);
        Vector2 Brot = Vector2.Transform(B, rotMatrix);

        Vector2 XArot = Arot - Xrot;
        Vector2 XBrot = Brot - Xrot;
        Vector2 XProt = P - Xrot;

        float clampedX = Math.Clamp(Vector2.Dot(XArot, XProt) / XArot.Length(), 0f, XArot.Length());
        float clampedY = Math.Clamp(Vector2.Dot(XBrot, XProt) / XBrot.Length(), 0f, XBrot.Length());
        return new Vector2(clampedX, clampedY) + Xrot;
    }

    public static Vector2 Flip(Vector2 vector)
    {
        return new Vector2(vector.Y, vector.X);
    }
}