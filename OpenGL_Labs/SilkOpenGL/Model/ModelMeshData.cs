using Silk.NET.OpenGL;

namespace SilkOpenGL.Model;

public sealed class ModelMeshData
{
    public ModelMeshData(
        string name,
        float[] vertices,
        uint[] indices,
        uint materialIndex = 0,
        string? materialKey = null,
        string? textureKey = null,
        GLEnum drawPrimitive = GLEnum.Triangles )
    {
        Name = name;
        Vertices = vertices;
        Indices = indices;
        MaterialIndex = materialIndex;
        MaterialKey = materialKey;
        TextureKey = textureKey;
        DrawPrimitive = drawPrimitive;
    }

    public string Name { get; }

    // Position (3), normal (3), UV (2)
    public float[] Vertices { get; }

    public uint[] Indices { get; }

    public uint MaterialIndex { get; }

    public string? MaterialKey { get; init; }

    public string? TextureKey { get; init; }

    public GLEnum DrawPrimitive { get; }

    public int VertexStride => 8;
}