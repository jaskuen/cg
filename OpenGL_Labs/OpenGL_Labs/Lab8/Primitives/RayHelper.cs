using System.Numerics;
using SilkOpenGL.RayTracing;

namespace Lab8.Primitives;

public static class RayHelper
{
    public static RayMaterial Default { get; } = new(
        new Vector3(0.85f, 0.85f, 0.85f),
        new Vector3(0.08f),
        new Vector3(0.9f),
        new Vector3(0.6f),
        32f,
        0f);
}