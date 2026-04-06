using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL.Objects;
using SilkOpenGL.Store;

namespace SilkOpenGL.Model;

public class ModelObject : RenderableObject
{
    private readonly ModelData _modelData;
    private readonly List<RenderableMesh> _meshes = [];
    private readonly string? _defaultTextureKey;
    private readonly string? _defaultMaterialKey;
    private readonly bool _useDefaultMaterial;
    private TextureStore? _textureStore;
    private MaterialStore? _materialStore;

    public bool EnableFaceCulling { get; set; }
    public GLEnum CullFaceMode { get; set; } = GLEnum.Back;
    public FrontFaceDirection FrontFaceDirection { get; set; } = FrontFaceDirection.Ccw;
    public bool EnableDepthWrite { get; set; } = true;
    public int? RenderOnlyMeshIndex { get; set; }
    public bool EnablePolygonOffsetFill { get; set; }
    public float PolygonOffsetFactor { get; set; } = 1.0f;
    public float PolygonOffsetUnits { get; set; } = 1.0f;
    public bool RenderWireframe { get; set; }
    public ISet<int> WireframeMeshIndices { get; } = new HashSet<int>();
    public IDictionary<int, Vector3> MeshPositionOffsets { get; } = new Dictionary<int, Vector3>();
    public IDictionary<int, float> MeshVertexZDebugSteps { get; } = new Dictionary<int, float>();

    public ModelObject( string shaderKey, ModelData modelData ) : base( shaderKey )
    {
        _modelData = modelData ?? throw new ArgumentNullException( nameof( modelData ) );
    }

    public ModelObject( string shaderKey, ModelData modelData, string resourceKey, bool isMaterial )
        : this( shaderKey, modelData )
    {
        if ( isMaterial )
        {
            _defaultMaterialKey = resourceKey;
            _useDefaultMaterial = true;
        }
        else
        {
            _defaultTextureKey = resourceKey;
        }
    }

    public Transform Transform => _transform;

    public override void Init( ShaderStore shaderStore, TextureStore textureStore, MaterialStore materialStore, GL gl )
    {
        _textureStore = textureStore;
        _materialStore = materialStore;
        base.Init( shaderStore, textureStore, materialStore, gl );
    }

    protected override void OnInit()
    {
        for ( int meshIndex = 0; meshIndex < _modelData.Meshes.Count; meshIndex++ )
        {
            ModelMeshData meshData = _modelData.Meshes[meshIndex];
            string? textureKey = _useDefaultMaterial ? null : meshData.TextureKey ?? _defaultTextureKey;
            string? materialKey =
                _useDefaultMaterial ? meshData.MaterialKey ?? _defaultMaterialKey : meshData.MaterialKey;

            if ( textureKey != null &&
                 _textureStore != null &&
                 !_textureStore.AllTextures.ContainsKey( textureKey ) &&
                 File.Exists( textureKey ) )
            {
                _textureStore.CreateTexture( textureKey, textureKey );
            }

            Texture? texture =
                textureKey != null && _textureStore != null && _textureStore.AllTextures.ContainsKey( textureKey )
                    ? _textureStore.GetTexture( textureKey )
                    : null;
            Material? material =
                materialKey != null && _materialStore != null && _materialStore.AllMaterials.ContainsKey( materialKey )
                    ? _materialStore.GetMaterial( materialKey )
                    : null;

            ModelMeshData meshToRender = meshData;
            if ( MeshVertexZDebugSteps.TryGetValue( meshIndex, out float zStep ) && zStep != 0f )
            {
                meshToRender = CreateDebugZSteppedMesh( meshData, zStep );
            }

            _meshes.Add( new RenderableMesh( _gl, meshToRender, texture, material ) );
        }

        (_vertices, _indices) = BuildCombinedGeometry( _modelData.Meshes );
    }

    public override void BindResources()
    {
        // ModelObject binds all mesh state explicitly during OnRender.
    }

    public override void OnUpdate( double dt )
    {
    }

    public override unsafe void OnRender( double dt )
    {
        for ( int meshIndex = 0; meshIndex < _meshes.Count; meshIndex++ )
        {
            if ( RenderOnlyMeshIndex.HasValue && RenderOnlyMeshIndex.Value != meshIndex )
            {
                continue;
            }

            RenderableMesh mesh = _meshes[meshIndex];
            _gl.Enable( EnableCap.DepthTest );
            bool renderMeshWireframe = RenderWireframe || WireframeMeshIndices.Contains( meshIndex );
            ApplyRenderState( renderMeshWireframe );
            _gl.DepthMask( EnableDepthWrite );

            Matrix4x4 meshModel = GetMeshModelMatrix( meshIndex );
            Matrix4x4.Invert( meshModel, out Matrix4x4 inverseModel );
            Matrix4x4 normalMatrix = Matrix4x4.Transpose( inverseModel );

            _shader.Use();
            _shader.SetUniform( "uModel", meshModel );
            _shader.TrySetUniform( "uNormalMatrix", normalMatrix );
            mesh.Bind();
            ResetMaterialUniforms();
            BindMeshResources( mesh );
            _gl.DrawElements( mesh.PrimitiveType, ( uint )mesh.IndexCount, DrawElementsType.UnsignedInt, null );
        }

        RestoreRenderState();
        _gl.DepthMask( true );
    }

    public override unsafe void OnRenderPicking( GL gl, Shader pickingShader )
    {
        pickingShader.Use();
        pickingShader.SetUniform( "uModel", _transform.ModelMatrix );

        for ( int meshIndex = 0; meshIndex < _meshes.Count; meshIndex++ )
        {
            if ( RenderOnlyMeshIndex.HasValue && RenderOnlyMeshIndex.Value != meshIndex )
            {
                continue;
            }

            RenderableMesh mesh = _meshes[meshIndex];
            mesh.Bind();
            gl.DrawElements( mesh.PrimitiveType, ( uint )mesh.IndexCount, DrawElementsType.UnsignedInt, null );
        }
    }

    public override void OnClose()
    {
        foreach ( RenderableMesh mesh in _meshes )
        {
            mesh.Dispose();
        }

        _meshes.Clear();
        _vertices = [];
        _indices = [];

        base.OnClose();
    }

    private void BindMeshResources( RenderableMesh mesh )
    {
        if ( mesh.Texture != null )
        {
            mesh.Texture.Bind();
            _shader.TrySetUniform( "uTexture", 0 );
            _shader.TrySetUniform( "uTextureId", mesh.Texture.TextureId );
            _shader.TrySetUniform( "uHandle", mesh.Texture.TextureId );
            _shader.TrySetUniform( "uHasTexture", 1 );
        }
        else
        {
            _gl.BindTexture( TextureTarget.Texture2D, 0 );
            _shader.TrySetUniform( "uTexture", 0 );
            _shader.TrySetUniform( "uTextureId", 0 );
            _shader.TrySetUniform( "uHandle", 0 );
            _shader.TrySetUniform( "uHasTexture", 0 );
        }

        Material? material = mesh.Material;
        if ( material == null )
        {
            return;
        }

        if ( material.Albedo != null )
        {
            _shader.TrySetUniform( "uMaterial.albedoMap", material.Albedo.TextureId );
            _shader.TrySetUniform( "uMaterial.hasAlbedoMap", 1 );
        }
        else
        {
            _shader.TrySetUniform( "uMaterial.hasAlbedoMap", 0 );
        }

        if ( material.Normal != null )
        {
            _shader.TrySetUniform( "uMaterial.normalMap", material.Normal.TextureId );
            _shader.TrySetUniform( "uMaterial.hasNormalMap", 1 );
        }
        else
        {
            _shader.TrySetUniform( "uMaterial.hasNormalMap", 0 );
        }

        if ( material.Metallic != null )
        {
            _shader.TrySetUniform( "uMaterial.metallicMap", material.Metallic.TextureId );
            _shader.TrySetUniform( "uMaterial.hasMetallicMap", 1 );
        }
        else
        {
            _shader.TrySetUniform( "uMaterial.hasMetallicMap", 0 );
        }

        if ( material.Roughness != null )
        {
            _shader.TrySetUniform( "uMaterial.roughnessMap", material.Roughness.TextureId );
            _shader.TrySetUniform( "uMaterial.hasRoughnessMap", 1 );
        }
        else
        {
            _shader.TrySetUniform( "uMaterial.hasRoughnessMap", 0 );
        }

        if ( material.Ao != null )
        {
            _shader.TrySetUniform( "uMaterial.aoMap", material.Ao.TextureId );
            _shader.TrySetUniform( "uMaterial.hasAoMap", 1 );
        }
        else
        {
            _shader.TrySetUniform( "uMaterial.hasAoMap", 0 );
        }
    }

    private void ResetMaterialUniforms()
    {
        _gl.BindTexture( TextureTarget.Texture2D, 0 );
        _shader.TrySetUniform( "uTexture", 0 );
        _shader.TrySetUniform( "uTextureId", 0 );
        _shader.TrySetUniform( "uHandle", 0 );
        _shader.TrySetUniform( "uHasTexture", 0 );
        _shader.TrySetUniform( "uMaterial.albedoMap", 0 );
        _shader.TrySetUniform( "uMaterial.normalMap", 0 );
        _shader.TrySetUniform( "uMaterial.metallicMap", 0 );
        _shader.TrySetUniform( "uMaterial.roughnessMap", 0 );
        _shader.TrySetUniform( "uMaterial.aoMap", 0 );
        _shader.TrySetUniform( "uMaterial.hasAlbedoMap", 0 );
        _shader.TrySetUniform( "uMaterial.hasNormalMap", 0 );
        _shader.TrySetUniform( "uMaterial.hasMetallicMap", 0 );
        _shader.TrySetUniform( "uMaterial.hasRoughnessMap", 0 );
        _shader.TrySetUniform( "uMaterial.hasAoMap", 0 );
    }

    private void ApplyRenderState( bool renderWireframe )
    {
        if ( EnableFaceCulling )
        {
            _gl.Enable( EnableCap.CullFace );
            _gl.FrontFace( FrontFaceDirection );
            _gl.CullFace( CullFaceMode );
        }
        else
        {
            _gl.Disable( EnableCap.CullFace );
        }

        if ( EnablePolygonOffsetFill )
        {
            _gl.Enable( EnableCap.PolygonOffsetFill );
            _gl.PolygonOffset( PolygonOffsetFactor, PolygonOffsetUnits );
        }
        else
        {
            _gl.Disable( EnableCap.PolygonOffsetFill );
        }

        if ( renderWireframe )
        {
            _gl.PolygonMode( TriangleFace.FrontAndBack, PolygonMode.Line );
        }
        else
        {
            _gl.PolygonMode( TriangleFace.FrontAndBack, PolygonMode.Fill );
        }
    }

    private void RestoreRenderState()
    {
        if ( EnableFaceCulling )
        {
            _gl.Disable( EnableCap.CullFace );
        }

        if ( EnablePolygonOffsetFill )
        {
            _gl.Disable( EnableCap.PolygonOffsetFill );
        }

        _gl.PolygonMode( TriangleFace.FrontAndBack, PolygonMode.Fill );
    }

    private Matrix4x4 GetMeshModelMatrix( int meshIndex )
    {
        if ( !MeshPositionOffsets.TryGetValue( meshIndex, out Vector3 offset ) )
        {
            return _transform.ModelMatrix;
        }

        return Matrix4x4.CreateTranslation( offset ) * _transform.ModelMatrix;
    }

    private static ModelMeshData CreateDebugZSteppedMesh( ModelMeshData meshData, float zStep )
    {
        float[] vertices = (float[])meshData.Vertices.Clone();
        int stride = meshData.VertexStride;
        int vertexCount = vertices.Length / stride;

        for ( int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++ )
        {
            int offset = vertexIndex * stride;
            vertices[offset + 2] += vertexIndex * zStep;
        }

        return new ModelMeshData(
            meshData.Name,
            vertices,
            meshData.Indices,
            meshData.PrimitiveType,
            meshData.IndicesPerPrimitive,
            meshData.MaterialIndex,
            meshData.MaterialKey,
            meshData.TextureKey );
    }

    private static ( float[] Vertices, uint[] Indices ) BuildCombinedGeometry( IReadOnlyList<ModelMeshData> meshes )
    {
        List<float> vertices = [];
        List<uint> indices = [];
        uint vertexOffset = 0;

        foreach ( ModelMeshData mesh in meshes )
        {
            vertices.AddRange( mesh.Vertices );

            foreach ( uint index in mesh.Indices )
            {
                indices.Add( index + vertexOffset );
            }

            vertexOffset += ( uint )( mesh.Vertices.Length / mesh.VertexStride );
        }

        return ( vertices.ToArray(), indices.ToArray() );
    }
}
