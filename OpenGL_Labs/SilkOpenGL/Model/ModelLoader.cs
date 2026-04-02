using Silk.NET.Assimp;

namespace SilkOpenGL.Model;

public static unsafe class ModelLoader
{
    public static ModelData Load( string filePath )
    {
        if ( string.IsNullOrWhiteSpace( filePath ) )
        {
            throw new ArgumentException( "Model path cannot be empty.", nameof( filePath ) );
        }

        string fullPath = Path.GetFullPath( filePath );
        if ( !System.IO.File.Exists( fullPath ) )
        {
            throw new FileNotFoundException( $"Model file not found: {fullPath}", fullPath );
        }

        var assimp = Assimp.GetApi();
        uint flags = ( uint )(
            PostProcessSteps.Triangulate |
            PostProcessSteps.GenerateNormals |
            PostProcessSteps.FlipUVs |
            PostProcessSteps.PreTransformVertices );

        Scene* scene = null;

        try
        {
            scene = assimp.ImportFile( fullPath, flags );

            if ( scene == null || scene->MRootNode == null || ( scene->MFlags & ( uint )SceneFlags.Incomplete ) != 0 )
            {
                throw new InvalidOperationException( $"Failed to load model: {fullPath}" );
            }

            if ( scene->MNumMeshes == 0 || scene->MMeshes == null )
            {
                throw new InvalidOperationException( $"Model does not contain any meshes: {fullPath}" );
            }

            List<ModelMeshData> meshes = new( ( int )scene->MNumMeshes );

            for ( uint meshIndex = 0; meshIndex < scene->MNumMeshes; meshIndex++ )
            {
                Mesh* mesh = scene->MMeshes[meshIndex];
                if ( mesh == null )
                {
                    continue;
                }

                meshes.Add( ReadMesh( mesh, meshIndex ) );
            }

            if ( meshes.Count == 0 )
            {
                throw new InvalidOperationException( $"No readable meshes were found in: {fullPath}" );
            }

            return new ModelData( fullPath, meshes );
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

    public static void Load3DS( string filePath, out float[] vertexData, out uint[] indexData )
    {
        ModelData model = Load( filePath );

        List<float> vertices = [];
        List<uint> indices = [];
        uint vertexOffset = 0;

        foreach ( ModelMeshData mesh in model.Meshes )
        {
            vertices.AddRange( mesh.Vertices );

            foreach ( uint index in mesh.Indices )
            {
                indices.Add( index + vertexOffset );
            }

            vertexOffset += ( uint )( mesh.Vertices.Length / mesh.VertexStride );
        }

        vertexData = vertices.ToArray();
        indexData = indices.ToArray();
    }

    private static ModelMeshData ReadMesh( Mesh* mesh, uint meshIndex )
    {
        List<float> vertices = new( ( int )mesh->MNumVertices * 8 );
        List<uint> indices = new( ( int )mesh->MNumFaces * 3 );

        bool hasNormals = mesh->MNormals != null;
        bool hasUvChannel = mesh->MTextureCoords[0] != null;

        for ( uint vertexIndex = 0; vertexIndex < mesh->MNumVertices; vertexIndex++ )
        {
            vertices.Add( mesh->MVertices[vertexIndex].X );
            vertices.Add( mesh->MVertices[vertexIndex].Y );
            vertices.Add( mesh->MVertices[vertexIndex].Z );

            if ( hasNormals )
            {
                vertices.Add( mesh->MNormals[vertexIndex].X );
                vertices.Add( mesh->MNormals[vertexIndex].Y );
                vertices.Add( mesh->MNormals[vertexIndex].Z );
            }
            else
            {
                vertices.Add( 0f );
                vertices.Add( 0f );
                vertices.Add( 0f );
            }

            if ( hasUvChannel )
            {
                vertices.Add( mesh->MTextureCoords[0][vertexIndex].X );
                vertices.Add( mesh->MTextureCoords[0][vertexIndex].Y );
            }
            else
            {
                vertices.Add( 0f );
                vertices.Add( 0f );
            }
        }

        for ( uint faceIndex = 0; faceIndex < mesh->MNumFaces; faceIndex++ )
        {
            Face face = mesh->MFaces[faceIndex];
            for ( uint indexIndex = 0; indexIndex < face.MNumIndices; indexIndex++ )
            {
                indices.Add( face.MIndices[indexIndex] );
            }
        }

        return new ModelMeshData(
            $"Mesh_{meshIndex}",
            vertices.ToArray(),
            indices.ToArray(),
            mesh->MMaterialIndex );
    }
}