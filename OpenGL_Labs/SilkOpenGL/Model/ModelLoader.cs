using Assimp;
using GLPrimitiveType = Silk.NET.OpenGL.PrimitiveType;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using NumericsVector3 = System.Numerics.Vector3;

namespace SilkOpenGL.Model;

public static unsafe class ModelLoader
{
    private const int VertexStride = ModelMeshData.PackedVertexStride;
    private static readonly ImportedMaterial DefaultMaterial = new(null, NumericsVector3.One);

    public static ModelData Load(string filePath)
    {
        return Load(filePath, new ModelImportOptions());
    }

    public static ModelData Load(string filePath, ModelImportOptions? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        options ??= new ModelImportOptions();
        string modelPath = Path.GetFullPath(filePath);
        if (!System.IO.File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found: {modelPath}", modelPath);
        }

        AssimpContext assimp = new();

        try
        {
            Scene scene = assimp.ImportFile(modelPath, options.ToPostProcessFlags());
            ValidateScene(assimp, scene, modelPath);

            string modelDirectory = Path.GetDirectoryName(modelPath) ?? string.Empty;
            Dictionary<int, ImportedMaterial> materials = BuildMaterialMap(assimp, scene, modelDirectory);
            List<ModelMeshData> meshes = [];

            ProcessNode(scene.RootNode, scene, materials, meshes, NumericsMatrix4x4.Identity, options);

            if (meshes.Count == 0)
            {
                throw new InvalidOperationException($"No meshes were loaded from model: {modelPath}");
            }

            return new ModelData(modelPath, meshes);
        }
        finally
        {
            assimp.Dispose();
        }
    }

    private static void ValidateScene(AssimpContext assimp, Scene scene, string modelPath)
    {
        if (scene != null && scene.RootNode != null && (scene.SceneFlags & SceneFlags.Incomplete) == 0)
        {
            return;
        }

        throw new InvalidOperationException($"Failed to load model '{modelPath}'.");
    }

    private static Dictionary<int, ImportedMaterial> BuildMaterialMap(
        AssimpContext assimp,
        Scene scene,
        string modelDirectory)
    {
        Dictionary<int, ImportedMaterial> materials = [];
        if (scene.Materials == null)
        {
            return materials;
        }

        for (int materialIndex = 0; materialIndex < scene.MaterialCount; materialIndex++)
        {
            Assimp.Material material = scene.Materials[materialIndex];
            materials[materialIndex] = new ImportedMaterial(
                ResolveTexturePath(assimp, material, modelDirectory),
                GetDiffuseColor(material));
        }

        return materials;
    }

    private static void ProcessNode(
        Node node,
        Scene scene,
        IReadOnlyDictionary<int, ImportedMaterial> materials,
        ICollection<ModelMeshData> meshes,
        NumericsMatrix4x4 parentTransform,
        ModelImportOptions options)
    {
        if (node == null)
        {
            return;
        }

        NumericsMatrix4x4 localTransform = ToNumerics(node.Transform);
        NumericsMatrix4x4 globalTransform = localTransform * parentTransform;

        for (int meshSlot = 0; meshSlot < node.MeshCount; meshSlot++)
        {
            int meshIndex = node.MeshIndices[meshSlot];
            Mesh mesh = scene.Meshes[meshIndex];
            if (mesh == null)
            {
                continue;
            }

            foreach (ModelMeshData meshData in ProcessMesh(mesh, meshIndex, materials, globalTransform, options))
            {
                meshes.Add(meshData);
            }
        }

        for (int childIndex = 0; childIndex < node.ChildCount; childIndex++)
        {
            ProcessNode(node.Children[childIndex], scene, materials, meshes, globalTransform, options);
        }
    }

    private static IReadOnlyList<ModelMeshData> ProcessMesh(
        Mesh mesh,
        int meshIndex,
        IReadOnlyDictionary<int, ImportedMaterial> materials,
        NumericsMatrix4x4 localTransform,
        ModelImportOptions options)
    {
        List<ModelMeshData> meshData = [];

        if (mesh.Vertices == null || mesh.VertexCount == 0)
        {
            return meshData;
        }

        string meshName = GetMeshName(mesh, meshIndex);
        if (ShouldSkipMesh(mesh, meshName, options))
        {
            return meshData;
        }

        float[] vertices = ExtractVertices(mesh);
        ImportedMaterial material = materials.TryGetValue(mesh.MaterialIndex, out ImportedMaterial? importedMaterial)
            ? importedMaterial
            : DefaultMaterial;

        foreach ((GLPrimitiveType Primitive, int FaceIndexCount, bool Include) primitive in GetRequestedPrimitives(options))
        {
            if (!primitive.Include)
            {
                continue;
            }

            uint[] indices = ExtractIndices(mesh, primitive.FaceIndexCount);
            int primitiveCount = indices.Length / primitive.FaceIndexCount;
            if (primitiveCount == 0 ||
                (options.MinimumPrimitiveCount.HasValue && primitiveCount < options.MinimumPrimitiveCount.Value))
            {
                continue;
            }

            string primitiveName = primitive.Primitive == GLPrimitiveType.Triangles
                ? meshName
                : $"{meshName}_{primitive.Primitive}";

            meshData.Add(new ModelMeshData(
                primitiveName,
                vertices,
                indices,
                (uint)mesh.MaterialIndex,
                textureKey: material.TextureKey,
                diffuseColor: material.DiffuseColor,
                drawPrimitive: primitive.Primitive,
                localTransform: localTransform));
        }

        return meshData;
    }

    private static float[] ExtractVertices(Mesh mesh)
    {
        float[] vertices = new float[mesh.VertexCount * VertexStride];
        bool hasNormals = mesh.HasNormals;
        bool hasTextureCoords = mesh.HasTextureCoords(0);
        bool hasTangents = mesh.HasTangentBasis;

        for (int vertexIndex = 0; vertexIndex < mesh.VertexCount; vertexIndex++)
        {
            int offset = vertexIndex * VertexStride;

            vertices[offset] = mesh.Vertices[vertexIndex].X;
            vertices[offset + 1] = mesh.Vertices[vertexIndex].Y;
            vertices[offset + 2] = mesh.Vertices[vertexIndex].Z;

            if (hasNormals)
            {
                vertices[offset + 3] = mesh.Normals[vertexIndex].X;
                vertices[offset + 4] = mesh.Normals[vertexIndex].Y;
                vertices[offset + 5] = mesh.Normals[vertexIndex].Z;
            }

            if (hasTextureCoords)
            {
                vertices[offset + 6] = mesh.TextureCoordinateChannels[0][vertexIndex].X;
                vertices[offset + 7] = mesh.TextureCoordinateChannels[0][vertexIndex].Y;
            }

            if (hasTangents)
            {
                vertices[offset + 8] = mesh.Tangents[vertexIndex].X;
                vertices[offset + 9] = mesh.Tangents[vertexIndex].Y;
                vertices[offset + 10] = mesh.Tangents[vertexIndex].Z;

                vertices[offset + 11] = mesh.BiTangents[vertexIndex].X;
                vertices[offset + 12] = mesh.BiTangents[vertexIndex].Y;
                vertices[offset + 13] = mesh.BiTangents[vertexIndex].Z;
            }
        }

        return vertices;
    }

    private static uint[] ExtractIndices(Mesh mesh, int faceIndexCount)
    {
        List<uint> indices = new(mesh.FaceCount * faceIndexCount);

        for (int faceIndex = 0; faceIndex < mesh.FaceCount; faceIndex++)
        {
            Face face = mesh.Faces[faceIndex];
            if (face.IndexCount != faceIndexCount)
            {
                continue;
            }

            for (int index = 0; index < face.IndexCount; index++)
            {
                indices.Add((uint)face.Indices[index]);
            }
        }

        return indices.ToArray();
    }

    private static (GLPrimitiveType Primitive, int FaceIndexCount, bool Include)[] GetRequestedPrimitives(
        ModelImportOptions options)
    {
        return
        [
            (GLPrimitiveType.Triangles, 3, options.IncludeTriangles),
            (GLPrimitiveType.Lines, 2, options.IncludeLines),
            (GLPrimitiveType.Points, 1, options.IncludePoints)
        ];
    }

    private static bool ShouldSkipMesh(Mesh mesh, string meshName, ModelImportOptions options)
    {
        if (options.ExcludedMeshNameSubstrings.Any(excluded =>
                meshName.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!options.MaxMeshExtent.HasValue)
        {
            return false;
        }

        (NumericsVector3 min, NumericsVector3 max) = GetBounds(mesh);
        NumericsVector3 size = max - min;
        float largestExtent = Math.Max(size.X, Math.Max(size.Y, size.Z));
        return largestExtent > options.MaxMeshExtent.Value;
    }

    private static (NumericsVector3 Min, NumericsVector3 Max) GetBounds(Mesh mesh)
    {
        NumericsVector3 min = new(float.PositiveInfinity);
        NumericsVector3 max = new(float.NegativeInfinity);

        for (int i = 0; i < mesh.VertexCount; i++)
        {
            NumericsVector3 position = new(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z);
            min = NumericsVector3.Min(min, position);
            max = NumericsVector3.Max(max, position);
        }

        return (min, max);
    }

    private static NumericsMatrix4x4 ToNumerics(Assimp.Matrix4x4 matrix)
    {
        // Assimp stores column-vector transforms; System.Numerics is used here with row-vector composition.
        return new NumericsMatrix4x4(
            matrix.A1, matrix.B1, matrix.C1, matrix.D1,
            matrix.A2, matrix.B2, matrix.C2, matrix.D2,
            matrix.A3, matrix.B3, matrix.C3, matrix.D3,
            matrix.A4, matrix.B4, matrix.C4, matrix.D4);
    }

    private static string GetMeshName(Mesh mesh, int meshIndex)
    {
        string meshName = mesh.Name;
        return string.IsNullOrWhiteSpace(meshName) ? $"Mesh_{meshIndex}" : meshName;
    }

    private static string? ResolveTexturePath(
        AssimpContext assimp,
        Assimp.Material material,
        string modelDirectory)
    {
        if (material == null)
        {
            return null;
        }

        return TryGetMaterialTexture(assimp, material, TextureType.Diffuse, modelDirectory) ??
               TryGetMaterialTexture(assimp, material, TextureType.BaseColor, modelDirectory);
    }

    private static NumericsVector3 GetDiffuseColor(Assimp.Material material)
    {
        if (material is not { HasColorDiffuse: true })
        {
            return NumericsVector3.One;
        }

        Color4D diffuse = material.ColorDiffuse;
        return new NumericsVector3(
            Math.Clamp(diffuse.R, 0f, 1f),
            Math.Clamp(diffuse.G, 0f, 1f),
            Math.Clamp(diffuse.B, 0f, 1f));
    }

    private static string? TryGetMaterialTexture(
        AssimpContext assimp,
        Assimp.Material material,
        TextureType textureType,
        string modelDirectory)
    {
        if (material.GetMaterialTextureCount(textureType) == 0)
        {
            return null;
        }

        bool status = material.GetMaterialTexture(
            textureType,
            0,
            out TextureSlot texture);

        if (!status)
        {
            return null;
        }

        string? rawTexturePath = texture.FilePath;
        if (string.IsNullOrWhiteSpace(rawTexturePath) || rawTexturePath.StartsWith('*'))
        {
            return null;
        }

        string normalizedTexturePath = rawTexturePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        string absolutePath = Path.IsPathRooted(normalizedTexturePath)
            ? Path.GetFullPath(normalizedTexturePath)
            : Path.GetFullPath(Path.Combine(modelDirectory, normalizedTexturePath));

        if (System.IO.File.Exists(absolutePath))
        {
            return absolutePath;
        }

        string? fileName = Path.GetFileName(normalizedTexturePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        string siblingTexturePath = Path.GetFullPath(Path.Combine(modelDirectory, fileName));
        return System.IO.File.Exists(siblingTexturePath) ? siblingTexturePath : null;
    }

    private sealed record ImportedMaterial(string? TextureKey, NumericsVector3 DiffuseColor);
}
