using System.Numerics;
using Silk.NET.OpenGL;

namespace SilkOpenGL.Model;

public sealed class ModelMeshData
{
    public const int PackedVertexStride = 14;

    public ModelMeshData(
        string name,
        float[] vertices,
        uint[] indices,
        uint materialIndex = 0,
        string? materialKey = null,
        string? textureKey = null,
        Vector3? diffuseColor = null,
        PrimitiveType drawPrimitive = PrimitiveType.Triangles,
        Matrix4x4? localTransform = null )
    {
        Name = name;
        Vertices = vertices;
        Indices = indices;
        MaterialIndex = materialIndex;
        MaterialKey = materialKey;
        TextureKey = textureKey;
        DiffuseColor = diffuseColor ?? Vector3.One;
        DrawPrimitive = drawPrimitive;
        LocalTransform = localTransform ?? Matrix4x4.Identity;
    }

    public string Name { get; }

    // Position (3), normal (3), UV (2), tangent (3), bitangent (3)
    public float[] Vertices { get; }

    public uint[] Indices { get; }

    public uint MaterialIndex { get; }

    public string? MaterialKey { get; init; }

    public string? TextureKey { get; init; }

    public Vector3 DiffuseColor { get; }

    public PrimitiveType DrawPrimitive { get; }

    public Matrix4x4 LocalTransform { get; }

    public int VertexStride => PackedVertexStride;
}
