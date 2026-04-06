using GLPrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace SilkOpenGL.Model;

public sealed class ModelMeshData
{
    public ModelMeshData(
        string name,
        float[] vertices,
        uint[] indices,
        GLPrimitiveType primitiveType = GLPrimitiveType.Triangles,
        int indicesPerPrimitive = 3,
        uint materialIndex = 0,
        string? materialKey = null,
        string? textureKey = null )
    {
        Name = name;
        Vertices = vertices;
        Indices = indices;
        PrimitiveType = primitiveType;
        IndicesPerPrimitive = indicesPerPrimitive;
        MaterialIndex = materialIndex;
        MaterialKey = materialKey;
        TextureKey = textureKey;
    }

    public string Name { get; }

    // Position (3), normal (3), UV (2)
    public float[] Vertices { get; }

    public uint[] Indices { get; }

    public GLPrimitiveType PrimitiveType { get; }

    public int IndicesPerPrimitive { get; }

    public uint MaterialIndex { get; }

    public string? MaterialKey { get; init; }

    public string? TextureKey { get; init; }

    public int VertexStride => 8;
}
