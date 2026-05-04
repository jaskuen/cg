using System.Numerics;

namespace Lab6.SeaBattle.Rendering;

public static class MeshFactory
{
    public static PrimitiveMesh WaterPlane(float halfWidth, float halfDepth)
    {
        float[] vertices =
        [
            -halfWidth, 0f, -halfDepth, 0f, 1f, 0f, 0f, 0f,
             halfWidth, 0f, -halfDepth, 0f, 1f, 0f, 1f, 0f,
             halfWidth, 0f,  halfDepth, 0f, 1f, 0f, 1f, 1f,
            -halfWidth, 0f,  halfDepth, 0f, 1f, 0f, 0f, 1f
        ];
        uint[] indices = [0, 1, 2, 2, 3, 0];
        return new PrimitiveMesh(vertices, indices);
    }

    public static PrimitiveMesh Cylinder(int segments = 24)
    {
        List<float> vertices = [];
        List<uint> indices = [];
        float half = 0.5f;

        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.Tau * i / segments;
            float a1 = MathF.Tau * (i + 1) / segments;
            Vector3 n0 = new(MathF.Cos(a0), MathF.Sin(a0), 0f);
            Vector3 n1 = new(MathF.Cos(a1), MathF.Sin(a1), 0f);
            AddQuad(vertices, indices,
                new Vector3(n0.X, n0.Y, -half), new Vector3(n1.X, n1.Y, -half),
                new Vector3(n1.X, n1.Y, half), new Vector3(n0.X, n0.Y, half),
                Vector3.Normalize(n0 + n1));
            AddTriangle(vertices, indices, new Vector3(0f, 0f, -half), new Vector3(n0.X, n0.Y, -half), new Vector3(n1.X, n1.Y, -half), -Vector3.UnitZ);
            AddTriangle(vertices, indices, new Vector3(0f, 0f, half), new Vector3(n1.X, n1.Y, half), new Vector3(n0.X, n0.Y, half), Vector3.UnitZ);
        }

        return new PrimitiveMesh(vertices.ToArray(), indices.ToArray());
    }

    public static PrimitiveMesh Cone(int segments = 24)
    {
        List<float> vertices = [];
        List<uint> indices = [];
        Vector3 tip = new(0f, 0f, 0.58f);
        Vector3 center = new(0f, 0f, -0.5f);

        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.Tau * i / segments;
            float a1 = MathF.Tau * (i + 1) / segments;
            Vector3 p0 = new(MathF.Cos(a0), MathF.Sin(a0), -0.5f);
            Vector3 p1 = new(MathF.Cos(a1), MathF.Sin(a1), -0.5f);
            Vector3 normal = Vector3.Normalize(Vector3.Cross(p1 - tip, p0 - tip));
            AddTriangle(vertices, indices, tip, p0, p1, normal);
            AddTriangle(vertices, indices, center, p1, p0, -Vector3.UnitZ);
        }

        return new PrimitiveMesh(vertices.ToArray(), indices.ToArray());
    }

    public static PrimitiveMesh VerticalPlane(float halfWidth, float halfHeight)
    {
        float[] vertices =
        [
            -halfWidth, -halfHeight, 0f, 0f, 0f, 1f, 0f, 0f,
             halfWidth, -halfHeight, 0f, 0f, 0f, 1f, 1f, 0f,
             halfWidth,  halfHeight, 0f, 0f, 0f, 1f, 1f, 1f,
            -halfWidth,  halfHeight, 0f, 0f, 0f, 1f, 0f, 1f
        ];
        uint[] indices = [0, 1, 2, 2, 3, 0];
        return new PrimitiveMesh(vertices, indices);
    }

    private static PrimitiveMesh CreateBox()
    {
        List<float> vertices = [];
        List<uint> indices = [];
        Vector3[] corners =
        [
            new(-0.5f, -0.5f,  0.5f), new(0.5f, -0.5f,  0.5f), new(0.5f, 0.5f,  0.5f), new(-0.5f, 0.5f,  0.5f),
            new(-0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, -0.5f, -0.5f)
        ];

        AddQuad(vertices, indices, corners[0], corners[1], corners[2], corners[3], Vector3.UnitZ);
        AddQuad(vertices, indices, corners[4], corners[5], corners[6], corners[7], -Vector3.UnitZ);
        AddQuad(vertices, indices, corners[4], corners[0], corners[3], corners[5], -Vector3.UnitX);
        AddQuad(vertices, indices, corners[1], corners[7], corners[6], corners[2], Vector3.UnitX);
        AddQuad(vertices, indices, corners[3], corners[2], corners[6], corners[5], Vector3.UnitY);
        AddQuad(vertices, indices, corners[4], corners[7], corners[1], corners[0], -Vector3.UnitY);
        return new PrimitiveMesh(vertices.ToArray(), indices.ToArray());
    }

    private static void AddQuad(List<float> vertices, List<uint> indices, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        uint start = (uint)(vertices.Count / 8);
        AddVertex(vertices, a, normal, new Vector2(0f, 0f));
        AddVertex(vertices, b, normal, new Vector2(1f, 0f));
        AddVertex(vertices, c, normal, new Vector2(1f, 1f));
        AddVertex(vertices, d, normal, new Vector2(0f, 1f));
        indices.AddRange([start, start + 1, start + 2, start + 2, start + 3, start]);
    }

    private static void AddTriangle(List<float> vertices, List<uint> indices, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
    {
        uint start = (uint)(vertices.Count / 8);
        AddVertex(vertices, a, normal, new Vector2(0.5f, 1f));
        AddVertex(vertices, b, normal, new Vector2(0f, 0f));
        AddVertex(vertices, c, normal, new Vector2(1f, 0f));
        indices.AddRange([start, start + 1, start + 2]);
    }

    private static void AddVertex(List<float> vertices, Vector3 position, Vector3 normal, Vector2 uv)
    {
        vertices.AddRange([position.X, position.Y, position.Z, normal.X, normal.Y, normal.Z, uv.X, uv.Y]);
    }
}
