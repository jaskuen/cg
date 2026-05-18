namespace SilkOpenGL.RayTracing;

public interface IRayTraceable
{
    bool Intersect(in Ray ray, float minDistance, float maxDistance, out HitInfo hit);
}
