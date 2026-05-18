using System.Numerics;

namespace SilkOpenGL.RayTracing;

public class RayTracingSettings
{
    public int RenderWidth { get; set; } = 640;
    public int RenderHeight { get; set; } = 360;
    public int MaxReflectionDepth { get; set; } = 1;
    public float ShadowBias { get; set; } = 0.001f;
    public Vector3 BackgroundTop { get; set; } = new(0.42f, 0.58f, 0.74f);
    public Vector3 BackgroundBottom { get; set; } = new(0.08f, 0.1f, 0.12f);
}
