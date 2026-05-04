using System.Numerics;

namespace Lab6.SeaBattle.Game;

internal static class TransformHelpers
{
    public static Quaternion RotationBetween(Vector3 from, Vector3 to)
    {
        from = Vector3.Normalize(from);
        to = Vector3.Normalize(to);
        float dot = Vector3.Dot(from, to);
        if (dot > 0.9999f)
        {
            return Quaternion.Identity;
        }

        if (dot < -0.9999f)
        {
            Vector3 fallbackAxis = MathF.Abs(from.Y) < 0.95f ? Vector3.UnitY : Vector3.UnitX;
            return Quaternion.CreateFromAxisAngle(fallbackAxis, MathF.PI);
        }

        Vector3 axis = Vector3.Normalize(Vector3.Cross(from, to));
        float angle = MathF.Acos(Math.Clamp(dot, -1f, 1f));
        return Quaternion.CreateFromAxisAngle(axis, angle);
    }
}
