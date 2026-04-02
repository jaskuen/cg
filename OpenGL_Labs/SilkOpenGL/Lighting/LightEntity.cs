using System.Numerics;
using SilkOpenGL.Objects;

namespace SilkOpenGL.Lighting;

public class LightEntity : UpdateableObject
{
    public Vector3 Position { get; set; } = new(0f, 0f, 1f);
    public Vector3 Ambient { get; set; } = new(0.2f, 0.2f, 0.2f);
    public Vector3 Diffuse { get; set; } = new(1f, 1f, 1f);
    public Vector3 Specular { get; set; } = new(1f, 1f, 1f);

    public float Intensity { get; set; } = 1f;
    public float Constant { get; set; } = 1f;
    public float Linear { get; set; } = 0.09f;
    public float Quadratic { get; set; } = 0.032f;

    public bool Enabled { get; set; } = true;

    public LightEntity()
    {
    }

    public LightEntity(Vector3 position, Vector3 diffuse)
    {
        Position = position;
        Diffuse = diffuse;
    }

    public override void OnUpdate(double dt)
    {
    }
}
