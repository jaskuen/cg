using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;
using SilkOpenGL.RayTracing;

namespace Lab8.Primitives;

public class RayTracedTorus : RenderableObject, IRayTraceable
{
    private const int MajorSegments = 250;
    private const int MinorSegments = 160;
    private const int SearchSteps = 1024;
    private const int RootIterations = 12;

    private readonly float _majorRadius = 0.75f;
    private readonly float _minorRadius = 0.25f;
    private readonly RayMaterial _rayMaterial;

    public RayTracedTorus() : base(Program.ObjectShaderName)
    {
    }

    public RayTracedTorus(Vector3 position, float majorRadius, float minorRadius, RayMaterial rayMaterial) : this()
    {
        Transform.Position = position;
        _majorRadius = majorRadius;
        _minorRadius = minorRadius;
        _rayMaterial = rayMaterial;
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
        _vao.Bind();
        _ebo.Bind();
        _vbo.Bind();
        _shader.Use();
        _shader.SetUniform("uModel", Transform.ModelMatrix);
        _shader.TrySetUniform("uColor", _rayMaterial.BaseColor);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    public bool Intersect(in Ray ray, float minDistance, float maxDistance, out HitInfo hit)
    {
        hit = default;

        if (!TryToLocalRay(ray, out Ray localRay))
        {
            return false;
        }

        float outerRadius = _majorRadius + _minorRadius;
        if (!IntersectBoundingSphere(localRay, outerRadius, minDistance, maxDistance, out float start, out float end))
        {
            return false;
        }

        float previousDistance = start;
        float previousValue = Evaluate(localRay.At(previousDistance));
        float step = (end - start) / SearchSteps;
        float eps = 1e-4f;

        for (int i = 1; i <= SearchSteps; i++)
        {
            float currentDistance = i == SearchSteps ? end : start + step * i;
            float currentValue = Evaluate(localRay.At(currentDistance));

            if (previousValue * currentValue < 0f ||
                MathF.Abs(previousValue) < eps ||
                MathF.Abs(currentValue) < eps)
            {
                float distance = RefineRoot(localRay, previousDistance, currentDistance);
                Vector3 localPosition = localRay.At(distance);
                Vector3 worldPosition = Vector3.Transform(localPosition, Transform.ModelMatrix);
                float worldDistance = distance;

                if (worldDistance >= minDistance && worldDistance <= maxDistance)
                {
                    Matrix4x4.Invert(Transform.ModelMatrix, out Matrix4x4 inv);
                    Matrix4x4 normalMatrix = Matrix4x4.Transpose(inv);

                    Vector3 worldNormal =
                        Vector3.Normalize(Vector3.TransformNormal(Normal(localPosition), normalMatrix));
                    hit = new HitInfo
                    {
                        Distance = worldDistance,
                        Position = worldPosition,
                        Normal = worldNormal,
                        Material = _rayMaterial
                    };
                    return true;
                }
            }

            previousDistance = currentDistance;
            previousValue = currentValue;
        }

        return false;
    }

    public override void OnUpdate(double dt)
    {
    }

    private bool TryToLocalRay(in Ray ray, out Ray localRay)
    {
        localRay = default;

        if (!Matrix4x4.Invert(Transform.ModelMatrix, out Matrix4x4 inverseModel))
        {
            return false;
        }

        Vector3 origin = Vector3.Transform(ray.Origin, inverseModel);
        Vector3 target = Vector3.Transform(ray.Origin + ray.Direction, inverseModel);
        localRay = new Ray(origin, target - origin);
        return true;
    }

    private bool IntersectBoundingSphere(
        in Ray ray,
        float radius,
        float minDistance,
        float maxDistance,
        out float start,
        out float end)
    {
        start = 0f;
        end = 0f;

        float a = Vector3.Dot(ray.Direction, ray.Direction);
        float halfB = Vector3.Dot(ray.Origin, ray.Direction);
        float c = Vector3.Dot(ray.Origin, ray.Origin) - radius * radius;
        float discriminant = halfB * halfB - a * c;

        if (discriminant < 0f)
        {
            return false;
        }

        float sqrtDiscriminant = MathF.Sqrt(discriminant);
        start = MathF.Max(minDistance, (-halfB - sqrtDiscriminant) / a);
        end = MathF.Min(maxDistance, (-halfB + sqrtDiscriminant) / a);
        return end >= start;
    }

    private float RefineRoot(in Ray ray, float left, float right)
    {
        float leftValue = Evaluate(ray.At(left));

        for (int i = 0; i < RootIterations; i++)
        {
            float mid = (left + right) * 0.5f;
            float midValue = Evaluate(ray.At(mid));

            if (leftValue * midValue <= 0f)
            {
                right = mid;
            }
            else
            {
                left = mid;
                leftValue = midValue;
            }
        }

        return (left + right) * 0.5f;
    }

    private float Evaluate(Vector3 p)
    {
        float sum = Vector3.Dot(p, p) + _majorRadius * _majorRadius - _minorRadius * _minorRadius;
        return sum * sum - 4f * _majorRadius * _majorRadius * (p.X * p.X + p.Z * p.Z);
    }

    private Vector3 Normal(Vector3 p)
    {
        Vector2 xz = new Vector2(p.X, p.Z);

        float len = xz.Length();

        if (len < 1e-6f)
        {
            return Vector3.UnitY;
        }

        Vector2 circle = xz / len * _majorRadius;

        Vector3 center = new Vector3(circle.X, 0f, circle.Y);

        return Vector3.Normalize(p - center);
    }

    private void BuildMesh()
    {
        List<float> vertices = [];
        List<uint> indices = [];

        for (int major = 0; major <= MajorSegments; major++)
        {
            float u = major * 2f * MathF.PI / MajorSegments;
            float cosU = MathF.Cos(u);
            float sinU = MathF.Sin(u);

            for (int minor = 0; minor <= MinorSegments; minor++)
            {
                float v = minor * 2f * MathF.PI / MinorSegments;
                float cosV = MathF.Cos(v);
                float sinV = MathF.Sin(v);

                float x = (_majorRadius + _minorRadius * cosV) * cosU;
                float y = _minorRadius * sinV;
                float z = (_majorRadius + _minorRadius * cosV) * sinU;
                vertices.AddRange([x, y, z]);
            }
        }

        int row = MinorSegments + 1;

        for (int major = 0; major < MajorSegments; major++)
        {
            for (int minor = 0; minor < MinorSegments; minor++)
            {
                uint a = (uint)(major * row + minor);
                uint b = (uint)((major + 1) * row + minor);
                uint c = (uint)((major + 1) * row + minor + 1);
                uint d = (uint)(major * row + minor + 1);

                indices.AddRange([a, b, d, d, b, c]);
            }
        }

        _vertices = [.. vertices];
        _indices = [.. indices];
    }
}