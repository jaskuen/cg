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
            Dictionary<int, ImportedMaterial> materials = BuildMaterialMap(assimp, scene, modelDirectory, modelPath);
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
        string modelDirectory,
        string modelPath)
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
                ResolveTexturePath(assimp, scene, material, modelDirectory, modelPath),
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

            float[] primitiveVertices = vertices;
            uint[] indices = ExtractIndices(mesh, primitive.FaceIndexCount);
            if (options.JoinIdenticalVertices)
            {
                (primitiveVertices, indices) = JoinPackedVertices(vertices, indices);
            }

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
                primitiveVertices,
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

    private static (float[] Vertices, uint[] Indices) JoinPackedVertices(float[] vertices, uint[] indices)
    {
        Dictionary<PackedVertexKey, uint> remap = new(indices.Length);
        List<float> joinedVertices = new(vertices.Length);
        uint[] joinedIndices = new uint[indices.Length];

        for (int i = 0; i < indices.Length; i++)
        {
            uint oldIndex = indices[i];
            int oldOffset = checked((int)oldIndex * VertexStride);
            PackedVertexKey key = new(vertices, oldOffset);

            if (!remap.TryGetValue(key, out uint newIndex))
            {
                newIndex = (uint)(joinedVertices.Count / VertexStride);
                remap.Add(key, newIndex);

                for (int component = 0; component < VertexStride; component++)
                {
                    joinedVertices.Add(vertices[oldOffset + component]);
                }
            }

            joinedIndices[i] = newIndex;
        }

        return (joinedVertices.ToArray(), joinedIndices);
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
        Scene scene,
        Assimp.Material material,
        string modelDirectory,
        string modelPath)
    {
        if (material == null)
        {
            return null;
        }

        return TryGetMaterialTexture(assimp, scene, material, TextureType.Diffuse, modelDirectory, modelPath) ??
               TryGetMaterialTexture(assimp, scene, material, TextureType.BaseColor, modelDirectory, modelPath);
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
        Scene scene,
        Assimp.Material material,
        TextureType textureType,
        string modelDirectory,
        string modelPath)
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
        if (string.IsNullOrWhiteSpace(rawTexturePath))
        {
            return null;
        }

        if (rawTexturePath.StartsWith('*'))
        {
            return ExtractEmbeddedTexture(scene, rawTexturePath, texture.TextureIndex, modelPath);
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
        if (System.IO.File.Exists(siblingTexturePath))
        {
            return siblingTexturePath;
        }

        Console.WriteLine($"Model texture not found: '{rawTexturePath}'");
        return null;
    }

    private static string? ExtractEmbeddedTexture(Scene scene, string rawTexturePath, int textureIndex, string modelPath)
    {
        if (scene.Textures == null || scene.TextureCount == 0)
        {
            return null;
        }

        int index = textureIndex;
        if (rawTexturePath.Length > 1 &&
            int.TryParse(rawTexturePath[1..], out int parsedIndex))
        {
            index = parsedIndex;
        }

        EmbeddedTexture? embeddedTexture = index >= 0 && index < scene.TextureCount
            ? scene.Textures[index]
            : scene.Textures.FirstOrDefault(x => string.Equals(x.Filename, rawTexturePath, StringComparison.Ordinal));

        if (embeddedTexture == null)
        {
            Console.WriteLine($"Embedded model texture not found: '{rawTexturePath}'");
            return null;
        }

        string cacheDirectory = Path.Combine(
            Path.GetTempPath(),
            "OpenGL_Labs",
            "EmbeddedModelTextures",
            StableCacheKey(modelPath));
        Directory.CreateDirectory(cacheDirectory);

        string extension = EmbeddedTextureExtension(embeddedTexture);
        string fileName = string.IsNullOrWhiteSpace(embeddedTexture.Filename)
            ? $"texture_{index}{extension}"
            : $"{Path.GetFileNameWithoutExtension(embeddedTexture.Filename)}_{index}{extension}";
        string cachePath = Path.Combine(cacheDirectory, SanitizeFileName(fileName));

        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        if (embeddedTexture.HasCompressedData)
        {
            File.WriteAllBytes(cachePath, embeddedTexture.CompressedData);
            return cachePath;
        }

        if (embeddedTexture.HasNonCompressedData)
        {
            WriteBmp(cachePath, embeddedTexture.Width, embeddedTexture.Height, embeddedTexture.NonCompressedData);
            return cachePath;
        }

        return null;
    }

    private static string EmbeddedTextureExtension(EmbeddedTexture texture)
    {
        if (!string.IsNullOrWhiteSpace(texture.CompressedFormatHint))
        {
            string hint = texture.CompressedFormatHint.Trim().TrimStart('.');
            if (hint.Equals("jpeg", StringComparison.OrdinalIgnoreCase))
            {
                hint = "jpg";
            }

            return $".{hint.ToLowerInvariant()}";
        }

        string? filenameExtension = Path.GetExtension(texture.Filename);
        return string.IsNullOrWhiteSpace(filenameExtension) ? ".bmp" : filenameExtension;
    }

    private static string StableCacheKey(string value)
    {
        uint hash = 2166136261;
        foreach (char c in Path.GetFullPath(value))
        {
            hash ^= c;
            hash *= 16777619;
        }

        return hash.ToString("x8");
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid, '_');
        }

        return fileName;
    }

    private static void WriteBmp(string path, int width, int height, IReadOnlyList<Texel> texels)
    {
        int rowStride = width * 4;
        int pixelDataSize = rowStride * height;
        int fileSize = 14 + 40 + pixelDataSize;

        using FileStream stream = File.Create(path);
        using BinaryWriter writer = new(stream);

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(14 + 40);

        writer.Write(40);
        writer.Write(width);
        writer.Write(-height);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);

        for (int i = 0; i < width * height; i++)
        {
            Texel texel = texels[i];
            writer.Write(texel.B);
            writer.Write(texel.G);
            writer.Write(texel.R);
            writer.Write(texel.A);
        }
    }

    private sealed record ImportedMaterial(string? TextureKey, NumericsVector3 DiffuseColor);

    private readonly struct PackedVertexKey : IEquatable<PackedVertexKey>
    {
        private readonly int _v00;
        private readonly int _v01;
        private readonly int _v02;
        private readonly int _v03;
        private readonly int _v04;
        private readonly int _v05;
        private readonly int _v06;
        private readonly int _v07;
        private readonly int _v08;
        private readonly int _v09;
        private readonly int _v10;
        private readonly int _v11;
        private readonly int _v12;
        private readonly int _v13;

        public PackedVertexKey(float[] vertices, int offset)
        {
            _v00 = BitConverter.SingleToInt32Bits(vertices[offset]);
            _v01 = BitConverter.SingleToInt32Bits(vertices[offset + 1]);
            _v02 = BitConverter.SingleToInt32Bits(vertices[offset + 2]);
            _v03 = BitConverter.SingleToInt32Bits(vertices[offset + 3]);
            _v04 = BitConverter.SingleToInt32Bits(vertices[offset + 4]);
            _v05 = BitConverter.SingleToInt32Bits(vertices[offset + 5]);
            _v06 = BitConverter.SingleToInt32Bits(vertices[offset + 6]);
            _v07 = BitConverter.SingleToInt32Bits(vertices[offset + 7]);
            _v08 = BitConverter.SingleToInt32Bits(vertices[offset + 8]);
            _v09 = BitConverter.SingleToInt32Bits(vertices[offset + 9]);
            _v10 = BitConverter.SingleToInt32Bits(vertices[offset + 10]);
            _v11 = BitConverter.SingleToInt32Bits(vertices[offset + 11]);
            _v12 = BitConverter.SingleToInt32Bits(vertices[offset + 12]);
            _v13 = BitConverter.SingleToInt32Bits(vertices[offset + 13]);
        }

        public bool Equals(PackedVertexKey other)
        {
            return _v00 == other._v00 &&
                   _v01 == other._v01 &&
                   _v02 == other._v02 &&
                   _v03 == other._v03 &&
                   _v04 == other._v04 &&
                   _v05 == other._v05 &&
                   _v06 == other._v06 &&
                   _v07 == other._v07 &&
                   _v08 == other._v08 &&
                   _v09 == other._v09 &&
                   _v10 == other._v10 &&
                   _v11 == other._v11 &&
                   _v12 == other._v12 &&
                   _v13 == other._v13;
        }

        public override bool Equals(object? obj)
        {
            return obj is PackedVertexKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(_v00);
            hash.Add(_v01);
            hash.Add(_v02);
            hash.Add(_v03);
            hash.Add(_v04);
            hash.Add(_v05);
            hash.Add(_v06);
            hash.Add(_v07);
            hash.Add(_v08);
            hash.Add(_v09);
            hash.Add(_v10);
            hash.Add(_v11);
            hash.Add(_v12);
            hash.Add(_v13);
            return hash.ToHashCode();
        }
    }
}
