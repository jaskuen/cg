using Silk.NET.Assimp;

namespace SilkOpenGL.Model;

public static unsafe class ModelLoader
{
    private const int VertexStride = 8;

    public static ModelData Load( string filePath )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( filePath );

        string modelPath = Path.GetFullPath( filePath );
        if ( !System.IO.File.Exists( modelPath ) )
        {
            throw new FileNotFoundException( $"Model file not found: {modelPath}", modelPath );
        }

        Assimp assimp = Assimp.GetApi();
        Scene* scene = null;

        try
        {
            scene = assimp.ImportFile( modelPath, GetImportFlags() );
            ValidateScene( assimp, scene, modelPath );

            string modelDirectory = Path.GetDirectoryName( modelPath ) ?? string.Empty;
            Dictionary<uint, string?> materialTextures = BuildMaterialTextureMap( assimp, scene, modelDirectory );
            List<ModelMeshData> meshes = [];

            ProcessNode( scene->MRootNode, scene, materialTextures, meshes );

            if ( meshes.Count == 0 )
            {
                throw new InvalidOperationException( $"No meshes were loaded from model: {modelPath}" );
            }

            return new ModelData( modelPath, meshes );
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

    private static uint GetImportFlags()
    {
        return ( uint )(
            PostProcessSteps.Triangulate |
            PostProcessSteps.FlipUVs |
            PostProcessSteps.GenerateSmoothNormals |
            PostProcessSteps.ImproveCacheLocality |
            PostProcessSteps.FindInvalidData |
            PostProcessSteps.ValidateDataStructure );
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
        IReadOnlyDictionary<uint, string?> materialTextures,
        ICollection<ModelMeshData> meshes )
    {
        if ( node == null )
        {
            return;
        }

        for ( uint meshSlot = 0; meshSlot < node->MNumMeshes; meshSlot++ )
        {
            uint meshIndex = node->MMeshes[meshSlot];
            Mesh* mesh = scene->MMeshes[meshIndex];
            if ( mesh == null )
            {
                continue;
            }

            meshes.Add( ProcessMesh( mesh, meshIndex, materialTextures ) );
        }

        for ( uint childIndex = 0; childIndex < node->MNumChildren; childIndex++ )
        {
            ProcessNode( node->MChildren[childIndex], scene, materialTextures, meshes );
        }
    }

    private static ModelMeshData ProcessMesh(
        Mesh* mesh,
        uint meshIndex,
        IReadOnlyDictionary<uint, string?> materialTextures )
    {
        if ( mesh->MVertices == null || mesh->MNumVertices == 0 )
        {
            throw new InvalidOperationException( $"Mesh {meshIndex} does not contain any vertices." );
        }

        float[] vertices = ExtractVertices( mesh );
        uint[] indices = ExtractIndices( mesh, meshIndex );
        materialTextures.TryGetValue( mesh->MMaterialIndex, out string? textureKey );

        return new ModelMeshData(
            GetMeshName( mesh, meshIndex ),
            vertices,
            indices,
            mesh->MMaterialIndex,
            textureKey: textureKey );
    }

    private static float[] ExtractVertices( Mesh* mesh )
    {
        float[] vertices = new float[mesh->MNumVertices * VertexStride];
        bool hasNormals = mesh->MNormals != null;
        bool hasTextureCoords = mesh->MTextureCoords[0] != null;

        for ( uint vertexIndex = 0; vertexIndex < mesh->MNumVertices; vertexIndex++ )
        {
            int offset = ( int )vertexIndex * VertexStride;

            vertices[offset] = mesh->MVertices[vertexIndex].X;
            vertices[offset + 1] = mesh->MVertices[vertexIndex].Y;
            vertices[offset + 2] = mesh->MVertices[vertexIndex].Z;

            if ( hasNormals )
            {
                vertices[offset + 3] = mesh->MNormals[vertexIndex].X;
                vertices[offset + 4] = mesh->MNormals[vertexIndex].Y;
                vertices[offset + 5] = mesh->MNormals[vertexIndex].Z;
            }

            if ( hasTextureCoords )
            {
                vertices[offset + 6] = mesh->MTextureCoords[0][vertexIndex].X;
                vertices[offset + 7] = mesh->MTextureCoords[0][vertexIndex].Y;
            }
        }

        return vertices;
    }

    private static uint[] ExtractIndices( Mesh* mesh, uint meshIndex )
    {
        List<uint> indices = new( ( int )mesh->MNumFaces * 3 );

        for ( uint faceIndex = 0; faceIndex < mesh->MNumFaces; faceIndex++ )
        {
            Face face = mesh->MFaces[faceIndex];
            if ( face.MNumIndices != 3 )
            {
                throw new InvalidOperationException(
                    $"Mesh '{meshIndex}' contains a non-triangle face at index {faceIndex}." );
            }

            for ( uint index = 0; index < face.MNumIndices; index++ )
            {
                indices.Add( face.MIndices[index] );
            }
        }

        return indices.ToArray();
    }

    private static string GetMeshName( Mesh* mesh, uint meshIndex )
    {
        string meshName = mesh->MName.AsString;
        return string.IsNullOrWhiteSpace( meshName ) ? $"Mesh_{meshIndex}" : meshName;
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
}
