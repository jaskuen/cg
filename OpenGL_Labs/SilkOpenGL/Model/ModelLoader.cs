using System.Numerics;
using Silk.NET.Assimp;
using GLPrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace SilkOpenGL.Model;

public static unsafe class ModelLoader
{
    private const int VertexStride = 8;

    public static ModelData Load( string filePath )
    {
        return Load( filePath, ModelImportOptions.Default );
    }

    public static ModelData Load( string filePath, ModelImportOptions options )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( filePath );
        options ??= ModelImportOptions.Default;

        string modelPath = Path.GetFullPath( filePath );
        if ( !System.IO.File.Exists( modelPath ) )
        {
            throw new FileNotFoundException( $"Model file not found: {modelPath}", modelPath );
        }

        Assimp assimp = Assimp.GetApi();
        Scene* scene = null;

        try
        {
            scene = assimp.ImportFile( modelPath, options.ToAssimpPostProcessFlags() );
            ValidateScene( assimp, scene, modelPath );

            string modelDirectory = Path.GetDirectoryName( modelPath ) ?? string.Empty;
            Dictionary<uint, string?> materialTextures = BuildMaterialTextureMap( assimp, scene, modelDirectory );
            List<ModelMeshData> meshes = [];
            List<MeshDiagnostics> diagnostics = [];

            ProcessNode( scene->MRootNode, scene, Matrix4x4.Identity, options, materialTextures, meshes, diagnostics );

            if ( meshes.Count == 0 )
            {
                throw new InvalidOperationException( $"No meshes were loaded from model: {modelPath}" );
            }

            return new ModelData( modelPath, meshes, new ModelDiagnostics( diagnostics ) );
        }
        finally
        {
            if ( scene != null )
            {
                assimp.ReleaseImport( scene );
            }

            assimp.Dispose();
        }
    }

    private static void ValidateScene( Assimp assimp, Scene* scene, string modelPath )
    {
        if ( scene != null && scene->MRootNode != null && ( scene->MFlags & ( uint )SceneFlags.Incomplete ) == 0 )
        {
            return;
        }

        string importerError = assimp.GetErrorStringS();
        if ( string.IsNullOrWhiteSpace( importerError ) )
        {
            importerError = "Unknown Assimp import error.";
        }

        throw new InvalidOperationException( $"Failed to load model '{modelPath}'. {importerError}" );
    }

    private static Dictionary<uint, string?> BuildMaterialTextureMap(
        Assimp assimp,
        Scene* scene,
        string modelDirectory )
    {
        Dictionary<uint, string?> textures = [];
        if ( scene->MMaterials == null )
        {
            return textures;
        }

        for ( uint materialIndex = 0; materialIndex < scene->MNumMaterials; materialIndex++ )
        {
            Silk.NET.Assimp.Material* material = scene->MMaterials[materialIndex];
            textures[materialIndex] = ResolveTexturePath( assimp, material, modelDirectory );
        }

        return textures;
    }

    private static void ProcessNode(
        Node* node,
        Scene* scene,
        Matrix4x4 parentTransform,
        ModelImportOptions options,
        IReadOnlyDictionary<uint, string?> materialTextures,
        ICollection<ModelMeshData> meshes,
        ICollection<MeshDiagnostics> diagnostics )
    {
        if ( node == null )
        {
            return;
        }

        Matrix4x4 nodeTransform = node->MTransformation;
        Matrix4x4 globalTransform = Matrix4x4.Multiply( nodeTransform, parentTransform );

        for ( uint meshSlot = 0; meshSlot < node->MNumMeshes; meshSlot++ )
        {
            uint meshIndex = node->MMeshes[meshSlot];
            Mesh* mesh = scene->MMeshes[meshIndex];
            if ( mesh == null )
            {
                continue;
            }

            foreach ( ModelMeshData modelMesh in ProcessMesh( mesh, meshIndex, globalTransform, options, materialTextures ) )
            {
                meshes.Add( modelMesh );
                diagnostics.Add( BuildDiagnostics( modelMesh, meshIndex ) );
            }
        }

        for ( uint childIndex = 0; childIndex < node->MNumChildren; childIndex++ )
        {
            ProcessNode( node->MChildren[childIndex], scene, globalTransform, options, materialTextures, meshes, diagnostics );
        }
    }

    private static IReadOnlyList<ModelMeshData> ProcessMesh(
        Mesh* mesh,
        uint meshIndex,
        Matrix4x4 transform,
        ModelImportOptions options,
        IReadOnlyDictionary<uint, string?> materialTextures )
    {
        if ( mesh->MVertices == null || mesh->MNumVertices == 0 )
        {
            throw new InvalidOperationException( $"Mesh {meshIndex} does not contain any vertices." );
        }

        float[] vertices = ExtractVertices( mesh, transform );
        materialTextures.TryGetValue( mesh->MMaterialIndex, out string? textureKey );
        Dictionary<int, List<uint>> indicesByPrimitiveSize = ExtractIndicesByPrimitiveSize( mesh, meshIndex );
        List<ModelMeshData> modelMeshes = [];
        string meshName = GetMeshName( mesh, meshIndex );

        if ( !options.IncludesMeshName( meshName ) )
        {
            return modelMeshes;
        }

        if ( options.ExcludesMeshName( meshName ) )
        {
            return modelMeshes;
        }

        if ( options.MaxMeshExtent is float maxMeshExtent && CalculateMaxExtent( vertices ) > maxMeshExtent )
        {
            return modelMeshes;
        }

        foreach ( KeyValuePair<int, List<uint>> entry in indicesByPrimitiveSize.OrderBy( x => x.Key ) )
        {
            if ( entry.Value.Count == 0 )
            {
                continue;
            }

            int indicesPerPrimitive = entry.Key;
            if ( !options.IncludesPrimitiveSize( indicesPerPrimitive ) )
            {
                continue;
            }

            int primitiveCount = entry.Value.Count / indicesPerPrimitive;
            if ( options.MinimumPrimitiveCount is int minimumPrimitiveCount && primitiveCount < minimumPrimitiveCount )
            {
                continue;
            }

            modelMeshes.Add( new ModelMeshData(
                GetPrimitiveMeshName( meshName, indicesPerPrimitive, modelMeshes.Count ),
                vertices,
                entry.Value.ToArray(),
                GetPrimitiveType( indicesPerPrimitive ),
                indicesPerPrimitive,
                mesh->MMaterialIndex,
                textureKey: textureKey ) );
        }

        return modelMeshes;
    }

    private static float[] ExtractVertices( Mesh* mesh, Matrix4x4 transform )
    {
        float[] vertices = new float[mesh->MNumVertices * VertexStride];
        bool hasNormals = mesh->MNormals != null;
        bool hasTextureCoords = mesh->MTextureCoords[0] != null;
        Matrix4x4.Invert( transform, out Matrix4x4 inverseTransform );
        Matrix4x4 normalTransform = Matrix4x4.Transpose( inverseTransform );

        for ( uint vertexIndex = 0; vertexIndex < mesh->MNumVertices; vertexIndex++ )
        {
            int offset = ( int )vertexIndex * VertexStride;
            Vector3 position = Vector3.Transform(
                new Vector3(
                    mesh->MVertices[vertexIndex].X,
                    mesh->MVertices[vertexIndex].Y,
                    mesh->MVertices[vertexIndex].Z ),
                transform );

            vertices[offset] = position.X;
            vertices[offset + 1] = position.Y;
            vertices[offset + 2] = position.Z;

            if ( hasNormals )
            {
                Vector3 normal = Vector3.Normalize(
                    Vector3.TransformNormal(
                        new Vector3(
                            mesh->MNormals[vertexIndex].X,
                            mesh->MNormals[vertexIndex].Y,
                            mesh->MNormals[vertexIndex].Z ),
                        normalTransform ) );

                vertices[offset + 3] = normal.X;
                vertices[offset + 4] = normal.Y;
                vertices[offset + 5] = normal.Z;
            }

            if ( hasTextureCoords )
            {
                vertices[offset + 6] = mesh->MTextureCoords[0][vertexIndex].X;
                vertices[offset + 7] = mesh->MTextureCoords[0][vertexIndex].Y;
            }
        }

        return vertices;
    }

    private static float CalculateMaxExtent( float[] vertices )
    {
        if ( vertices.Length == 0 )
        {
            return 0f;
        }

        Vector3 min = new( float.MaxValue );
        Vector3 max = new( float.MinValue );

        for ( int offset = 0; offset < vertices.Length; offset += VertexStride )
        {
            Vector3 position = new( vertices[offset], vertices[offset + 1], vertices[offset + 2] );
            min = Vector3.Min( min, position );
            max = Vector3.Max( max, position );
        }

        Vector3 extents = max - min;
        return MathF.Max( extents.X, MathF.Max( extents.Y, extents.Z ) );
    }

    private static Dictionary<int, List<uint>> ExtractIndicesByPrimitiveSize( Mesh* mesh, uint meshIndex )
    {
        Dictionary<int, List<uint>> indices = [];

        for ( uint faceIndex = 0; faceIndex < mesh->MNumFaces; faceIndex++ )
        {
            Face face = mesh->MFaces[faceIndex];
            int primitiveSize = (int)face.MNumIndices;
            if ( primitiveSize is < 1 or > 3 )
            {
                throw new InvalidOperationException(
                    $"Mesh '{meshIndex}' contains an unsupported face with {face.MNumIndices} indices at face {faceIndex}." );
            }

            if ( !indices.TryGetValue( primitiveSize, out List<uint>? bucket ) )
            {
                bucket = [];
                indices[primitiveSize] = bucket;
            }

            for ( uint index = 0; index < face.MNumIndices; index++ )
            {
                bucket.Add( face.MIndices[index] );
            }
        }

        return indices;
    }

    private static string GetMeshName( Mesh* mesh, uint meshIndex )
    {
        string meshName = mesh->MName.AsString;
        return string.IsNullOrWhiteSpace( meshName ) ? $"Mesh_{meshIndex}" : meshName;
    }

    private static string GetPrimitiveMeshName( string meshName, int indicesPerPrimitive, int primitiveGroupIndex )
    {
        if ( primitiveGroupIndex == 0 && indicesPerPrimitive == 3 )
        {
            return meshName;
        }

        string suffix = indicesPerPrimitive switch
        {
            1 => "Points",
            2 => "Lines",
            3 => "Triangles",
            _ => $"Primitive{indicesPerPrimitive}"
        };

        return $"{meshName}_{suffix}";
    }

    private static GLPrimitiveType GetPrimitiveType( int indicesPerPrimitive )
    {
        return indicesPerPrimitive switch
        {
            1 => GLPrimitiveType.Points,
            2 => GLPrimitiveType.Lines,
            3 => GLPrimitiveType.Triangles,
            _ => throw new InvalidOperationException( $"Unsupported primitive size: {indicesPerPrimitive}" )
        };
    }

    private static string? ResolveTexturePath(
        Assimp assimp,
        Silk.NET.Assimp.Material* material,
        string modelDirectory )
    {
        if ( material == null )
        {
            return null;
        }

        return TryGetMaterialTexture( assimp, material, TextureType.Diffuse, modelDirectory ) ??
               TryGetMaterialTexture( assimp, material, TextureType.BaseColor, modelDirectory );
    }

    private static string? TryGetMaterialTexture(
        Assimp assimp,
        Silk.NET.Assimp.Material* material,
        TextureType textureType,
        string modelDirectory )
    {
        if ( assimp.GetMaterialTextureCount( material, textureType ) == 0 )
        {
            return null;
        }

        AssimpString texturePath = default;
        Return status = assimp.GetMaterialTexture(
            material,
            textureType,
            0,
            &texturePath,
            null,
            null,
            null,
            null,
            null,
            null );

        if ( status != Return.Success )
        {
            return null;
        }

        string? rawTexturePath = texturePath.AsString;
        if ( string.IsNullOrWhiteSpace( rawTexturePath ) || rawTexturePath.StartsWith( '*' ) )
        {
            return null;
        }

        string normalizedRelativePath = rawTexturePath
            .Replace( '\\', Path.DirectorySeparatorChar )
            .Replace( '/', Path.DirectorySeparatorChar );

        string absolutePath = Path.GetFullPath( Path.Combine( modelDirectory, normalizedRelativePath ) );
        return System.IO.File.Exists( absolutePath ) ? absolutePath : null;
    }

    private static MeshDiagnostics BuildDiagnostics( ModelMeshData mesh, uint meshIndex )
    {
        int stride = mesh.VertexStride;
        int vertexCount = mesh.Vertices.Length / stride;
        int primitiveCount = mesh.IndicesPerPrimitive == 0 ? 0 : mesh.Indices.Length / mesh.IndicesPerPrimitive;
        Vector3 boundsMin = new( float.MaxValue );
        Vector3 boundsMax = new( float.MinValue );
        float minTriangleArea = float.MaxValue;
        float maxTriangleArea = 0f;
        int degenerateTriangleCount = 0;

        for ( int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++ )
        {
            int offset = vertexIndex * stride;
            Vector3 position = new( mesh.Vertices[offset], mesh.Vertices[offset + 1], mesh.Vertices[offset + 2] );
            boundsMin = Vector3.Min( boundsMin, position );
            boundsMax = Vector3.Max( boundsMax, position );
        }

        if ( mesh.IndicesPerPrimitive == 3 )
        {
            for ( int triangleIndex = 0; triangleIndex < primitiveCount; triangleIndex++ )
            {
                int indexOffset = triangleIndex * 3;
                Vector3 a = ReadPosition( mesh, (int)mesh.Indices[indexOffset] );
                Vector3 b = ReadPosition( mesh, (int)mesh.Indices[indexOffset + 1] );
                Vector3 c = ReadPosition( mesh, (int)mesh.Indices[indexOffset + 2] );
                float area = Vector3.Cross( b - a, c - a ).Length() * 0.5f;
                minTriangleArea = MathF.Min( minTriangleArea, area );
                maxTriangleArea = MathF.Max( maxTriangleArea, area );

                if ( area < 1e-7f )
                {
                    degenerateTriangleCount++;
                }
            }
        }

        if ( primitiveCount == 0 || mesh.IndicesPerPrimitive != 3 )
        {
            minTriangleArea = 0f;
        }

        return new MeshDiagnostics
        {
            Name = mesh.Name,
            MeshIndex = (int)meshIndex,
            VertexCount = vertexCount,
            TriangleCount = primitiveCount,
            DegenerateTriangleCount = degenerateTriangleCount,
            BoundsMin = boundsMin,
            BoundsMax = boundsMax,
            MinTriangleArea = minTriangleArea,
            MaxTriangleArea = maxTriangleArea
        };
    }

    private static Vector3 ReadPosition( ModelMeshData mesh, int vertexIndex )
    {
        int offset = vertexIndex * mesh.VertexStride;
        return new Vector3( mesh.Vertices[offset], mesh.Vertices[offset + 1], mesh.Vertices[offset + 2] );
    }

}
