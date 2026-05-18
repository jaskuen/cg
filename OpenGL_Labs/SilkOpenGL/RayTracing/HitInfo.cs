using System.Numerics;

namespace SilkOpenGL.RayTracing;

public struct HitInfo
{
    public float Distance { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Normal { get; set; }
    public RayMaterial Material { get; set; }
}
