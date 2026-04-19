using Assimp;

namespace SilkOpenGL.Model;

public static unsafe class ModelLoader
{
    private const int VertexStride = 8;

    public static ModelData Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string modelPath = Path.GetFullPath(filePath);
        if (!System.IO.File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found: {modelPath}", modelPath);
        }

        AssimpContext assimp = new();

        try
        {
            Scene scene = assimp.ImportFile(modelPath, GetImportFlags());
            ValidateScene(assimp, scene, modelPath);

            string modelDirectory = Path.GetDirectoryName(modelPath) ?? string.Empty;
            Dictionary<int, string?> materialTextures = BuildMaterialTextureMap(assimp, scene, modelDirectory);
            List<ModelMeshData> meshes = [];

            ProcessNode(scene.RootNode, scene, materialTextures, meshes);

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

    private static PostProcessSteps GetImportFlags()
    {
        return PostProcessSteps.Triangulate;
        // |
        // PostProcessSteps.GenerateSmoothNormals |
        // PostProcessSteps.FlipUVs |
        // PostProcessSteps.CalculateTangentSpace |
        // PostProcessSteps.JoinIdenticalVertices;
    }

    private static void ValidateScene(AssimpContext assimp, Scene scene, string modelPath)
    {
        if (scene != null && scene.RootNode != null && (scene.SceneFlags & SceneFlags.Incomplete) == 0)
        {
            return;
        }

        throw new InvalidOperationException($"Failed to load model '{modelPath}'.");
    }

    private static Dictionary<int, string?> BuildMaterialTextureMap(
        AssimpContext assimp,
        Scene scene,
        string modelDirectory)
    {
        Dictionary<int, string?> textures = [];
        if (scene.Materials == null)
        {
            return textures;
        }

        for (int materialIndex = 0; materialIndex < scene.MaterialCount; materialIndex++)
        {
            Assimp.Material material = scene.Materials[materialIndex];
            textures[materialIndex] = ResolveTexturePath(assimp, material, modelDirectory);
        }

        return textures;
    }

    private static void ProcessNode(
        Node node,
        Scene scene,
        IReadOnlyDictionary<int, string?> materialTextures,
        ICollection<ModelMeshData> meshes)
    {
        if (node == null)
        {
            return;
        }
        
        Console.WriteLine(node.Transform.ToString());

        for (int meshSlot = 0; meshSlot < node.MeshCount; meshSlot++)
        {
            int meshIndex = node.MeshIndices[meshSlot];
            Mesh mesh = scene.Meshes[meshIndex];
            if (mesh == null)
            {
                continue;
            }

            meshes.Add(ProcessMesh(mesh, meshIndex, materialTextures));
        }

        for (int childIndex = 0; childIndex < node.ChildCount; childIndex++)
        {
            ProcessNode(node.Children[childIndex], scene, materialTextures, meshes);
        }
    }

    private static ModelMeshData ProcessMesh(
        Mesh mesh,
        int meshIndex,
        IReadOnlyDictionary<int, string?> materialTextures)
    {
        if (mesh.Vertices == null || mesh.VertexCount == 0)
        {
            throw new InvalidOperationException($"Mesh {meshIndex} does not contain any vertices.");
        }
        
        float[] vertices = ExtractVertices(mesh);
        uint[] indices = ExtractIndices(mesh, meshIndex);
        materialTextures.TryGetValue(mesh.MaterialIndex, out string? textureKey);

        return new ModelMeshData(
            GetMeshName(mesh, meshIndex),
            vertices,
            indices,
            (uint)mesh.MaterialIndex,
            textureKey: textureKey);
    }

    private static float[] ExtractVertices(Mesh mesh)
    {
        float[] vertices = new float[mesh.VertexCount * VertexStride];
        bool hasNormals = mesh.Normals != null;
        bool hasTextureCoords = mesh.TextureCoordinateChannels[0] != null;

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
        }

        return vertices;
    }

    private static uint[] ExtractIndices(Mesh mesh, int meshIndex)
    {
        List<uint> indices = new(mesh.FaceCount * 3);

        for (int faceIndex = 0; faceIndex < mesh.FaceCount; faceIndex++)
        {
            Face face = mesh.Faces[faceIndex];
            if (face.IndexCount != 3)
            {
                throw new InvalidOperationException(
                    $"Mesh '{meshIndex}' contains a non-triangle face at index {faceIndex}.");
            }

            for (int index = 0; index < face.IndexCount; index++)
            {
                indices.Add((uint)face.Indices[index]);
            }
        }

        return indices.ToArray();
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

        string normalizedRelativePath = rawTexturePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        string absolutePath = Path.GetFullPath(Path.Combine(modelDirectory, normalizedRelativePath));
        return System.IO.File.Exists(absolutePath) ? absolutePath : null;
    }
}