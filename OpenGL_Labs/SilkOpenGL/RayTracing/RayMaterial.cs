using System.Numerics;

namespace SilkOpenGL.RayTracing;

public readonly struct RayMaterial
{
    public RayMaterial(
        Vector3 baseColor,
        Vector3 ambient,
        Vector3 diffuse,
        Vector3 specular,
        float shininess = 32f,
        float reflectivity = 0f)
    {
        BaseColor = baseColor;
        Ambient = ambient;
        Diffuse = diffuse;
        Specular = specular;
        Shininess = shininess;
        Reflectivity = Math.Clamp(reflectivity, 0f, 1f);
    }

    public Vector3 BaseColor { get; init; }
    public Vector3 Ambient { get; init; }
    public Vector3 Diffuse { get; init; }
    public Vector3 Specular { get; init; }
    public float Shininess { get; init; }
    public float Reflectivity { get; init; }
}
