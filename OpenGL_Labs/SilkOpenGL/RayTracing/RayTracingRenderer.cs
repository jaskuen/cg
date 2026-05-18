using System.Numerics;
using Silk.NET.Maths;
using SilkOpenGL.Camera;
using SilkOpenGL.Lighting;

namespace SilkOpenGL.RayTracing;

public class RayTracingRenderer
{
    private const float MinHitDistance = 0.001f;
    private readonly RayTracingSettings _settings;
    private byte[] _framebuffer = [];

    public RayTracingRenderer(RayTracingSettings settings)
    {
        _settings = settings;
    }

    public byte[] Render(
        CameraObject camera,
        IReadOnlyList<IRayTraceable> objects,
        IReadOnlyList<LightEntity> lights,
        Vector2D<int> windowSize)
    {
        int width = Math.Max(1, _settings.RenderWidth);
        int height = Math.Max(1, _settings.RenderHeight);
        int requiredLength = width * height * 4;

        if (_framebuffer.Length != requiredLength)
        {
            _framebuffer = new byte[requiredLength];
        }

        float aspect = (float)windowSize.X / Math.Max(1, windowSize.Y);
        Matrix4x4 viewProjection = camera.ViewMatrix * camera.ProjectionMatrix(aspect);
        Matrix4x4.Invert(viewProjection, out Matrix4x4 inverseViewProjection);

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                Ray ray = CreatePrimaryRay(camera.Position, inverseViewProjection, x, y, width, height);
                Vector3 color = Trace(ray, objects, lights, 0);
                WritePixel(x, y, width, color);
            }
        });

        return _framebuffer;
    }

    private Ray CreatePrimaryRay(Vector3 origin, Matrix4x4 inverseViewProjection, int x, int y, int width, int height)
    {
        float ndcX = ((x + 0.5f) / width) * 2f - 1f;
        float ndcY = 1f - ((y + 0.5f) / height) * 2f;

        Vector4 near = Vector4.Transform(new Vector4(ndcX, ndcY, 0f, 1f), inverseViewProjection);
        Vector4 far = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), inverseViewProjection);
        near /= near.W;
        far /= far.W;

        Vector3 direction = new Vector3(far.X - near.X, far.Y - near.Y, far.Z - near.Z);
        direction = Vector3.Normalize(direction);
        return new Ray(origin, direction);
    }

    private Vector3 Trace(
        in Ray ray,
        IReadOnlyList<IRayTraceable> objects,
        IReadOnlyList<LightEntity> lights,
        int depth)
    {
        if (!TryFindClosest(ray, objects, MinHitDistance, float.PositiveInfinity, out HitInfo hit))
        {
            return Background(ray.Direction);
        }

        Vector3 color = Shade(ray, hit, objects, lights);

        if (depth < _settings.MaxReflectionDepth && hit.Material.Reflectivity > 0f)
        {
            Vector3 reflectedDirection = Vector3.Reflect(ray.Direction, hit.Normal);
            Ray reflectedRay = new(hit.Position + hit.Normal * _settings.ShadowBias, reflectedDirection);
            Vector3 reflectedColor = Trace(reflectedRay, objects, lights, depth + 1);
            color = Vector3.Lerp(color, reflectedColor, hit.Material.Reflectivity);
        }

        return Clamp01(color);
    }

    private Vector3 Shade(
        in Ray ray,
        HitInfo hit,
        IReadOnlyList<IRayTraceable> objects,
        IReadOnlyList<LightEntity> lights)
    {
        RayMaterial material = hit.Material;
        Vector3 result = material.BaseColor * material.Ambient;
        Vector3 viewDirection = Vector3.Normalize(-ray.Direction);

        foreach (LightEntity light in lights)
        {
            if (!light.Enabled)
            {
                continue;
            }

            Vector3 toLight = light.Position - hit.Position;
            float lightDistance = toLight.Length();

            if (lightDistance <= 0.0001f)
            {
                continue;
            }

            Vector3 lightDirection = toLight / lightDistance;
            Ray shadowRay = new(hit.Position + hit.Normal * _settings.ShadowBias, lightDirection);

            if (TryFindClosest(shadowRay, objects, MinHitDistance, lightDistance - _settings.ShadowBias, out _))
            {
                continue;
            }

            float attenuation = light.Intensity /
                MathF.Max(0.0001f, light.Constant + light.Linear * lightDistance + light.Quadratic * lightDistance * lightDistance);

            float diffuseFactor = MathF.Max(Vector3.Dot(hit.Normal, lightDirection), 0f);
            Vector3 diffuse = material.BaseColor * material.Diffuse * light.Diffuse * diffuseFactor;

            Vector3 reflected = Vector3.Reflect(-lightDirection, hit.Normal);
            float specularFactor = MathF.Pow(MathF.Max(Vector3.Dot(viewDirection, reflected), 0f), material.Shininess);
            Vector3 specular = material.Specular * light.Specular * specularFactor;

            result += (light.Ambient * material.Ambient * material.BaseColor + diffuse + specular) * attenuation;
        }

        return result;
    }

    private bool TryFindClosest(
        in Ray ray,
        IReadOnlyList<IRayTraceable> objects,
        float minDistance,
        float maxDistance,
        out HitInfo closestHit)
    {
        closestHit = default;
        bool hasHit = false;
        float closestDistance = maxDistance;

        foreach (IRayTraceable obj in objects)
        {
            if (obj.Intersect(ray, minDistance, closestDistance, out HitInfo hit))
            {
                hasHit = true;
                closestDistance = hit.Distance;
                closestHit = hit;
            }
        }

        return hasHit;
    }

    private Vector3 Background(Vector3 direction)
    {
        float t = Math.Clamp(direction.Y * 0.5f + 0.5f, 0f, 1f);
        return Vector3.Lerp(_settings.BackgroundBottom, _settings.BackgroundTop, t);
    }

    private void WritePixel(int x, int y, int width, Vector3 color)
    {
        color = Clamp01(color);
        int offset = (y * width + x) * 4;
        _framebuffer[offset] = ToByte(color.X);
        _framebuffer[offset + 1] = ToByte(color.Y);
        _framebuffer[offset + 2] = ToByte(color.Z);
        _framebuffer[offset + 3] = 255;
    }

    private static Vector3 Clamp01(Vector3 value)
    {
        return new Vector3(
            Math.Clamp(value.X, 0f, 1f),
            Math.Clamp(value.Y, 0f, 1f),
            Math.Clamp(value.Z, 0f, 1f));
    }

    private static byte ToByte(float value)
    {
        return (byte)(Math.Clamp(value, 0f, 1f) * 255f);
    }
}
