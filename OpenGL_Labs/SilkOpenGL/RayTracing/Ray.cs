using System.Numerics;

namespace SilkOpenGL.RayTracing;

public readonly struct Ray
{
    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = Vector3.Normalize(direction);
    }

    public Vector3 Origin { get; }
    public Vector3 Direction { get; }

    public Vector3 At(float distance) => Origin + Direction * distance;
}
