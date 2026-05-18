using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;
using SilkOpenGL.RayTracing;

namespace Lab8.Primitives;

public class RayTracedPlane : RenderableObject, IRayTraceable
{
    public Vector3 Normal { get; set; } = Vector3.UnitY;
    public RayMaterial Material { get; set; }
    public float PreviewSize { get; set; } = 24f;

    public RayTracedPlane() : base(Program.ObjectShaderName)
    {
    }

    public RayTracedPlane(Vector3 position, Vector3 normal, RayMaterial material) : this()
    {
        Transform.Position = position;
        Normal = Vector3.Normalize(normal);
        Material = material;
    }

    protected override void OnInit()
    {
        BuildMesh();

        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
    }

    public override unsafe void OnRender(double dt)
    {
        _shader.Use();
        _shader.SetUniform("uModel", Transform.ModelMatrix);
        _shader.TrySetUniform("uColor", Material.BaseColor);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    public bool Intersect(in Ray ray, float minDistance, float maxDistance, out HitInfo hit)
    {
        hit = default;

        Vector3 normal = Vector3.Normalize(Normal);
        float denominator = Vector3.Dot(normal, ray.Direction);

        if (MathF.Abs(denominator) < 0.0001f)
        {
            return false;
        }

        float distance = Vector3.Dot(Transform.Position - ray.Origin, normal) / denominator;

        if (distance < minDistance || distance > maxDistance)
        {
            return false;
        }

        if (denominator > 0f)
        {
            normal = -normal;
        }

        hit = new HitInfo
        {
            Distance = distance,
            Position = ray.At(distance),
            Normal = normal,
            Material = Material
        };

        return true;
    }

    public override void OnUpdate(double dt)
    {
    }

    private void BuildMesh()
    {
        Vector3 normal = Vector3.Normalize(Normal);
        Vector3 tangent = MathF.Abs(Vector3.Dot(normal, Vector3.UnitY)) > 0.95f
            ? Vector3.Normalize(Vector3.Cross(normal, Vector3.UnitX))
            : Vector3.Normalize(Vector3.Cross(normal, Vector3.UnitY));
        Vector3 bitangent = Vector3.Normalize(Vector3.Cross(normal, tangent));
        float half = PreviewSize * 0.5f;

        Vector3 a = (-tangent - bitangent) * half;
        Vector3 b = (tangent - bitangent) * half;
        Vector3 c = (tangent + bitangent) * half;
        Vector3 d = (-tangent + bitangent) * half;

        _vertices =
        [
            a.X, a.Y, a.Z,
            b.X, b.Y, b.Z,
            c.X, c.Y, c.Z,
            d.X, d.Y, d.Z
        ];

        _indices = [0, 1, 2, 2, 3, 0];
    }
}
