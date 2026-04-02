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
    private bool _isInitialized;

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
        if ( _isInitialized )
        {
            return;
        }

        _gl = gl;
        _shader = shaderStore.GetShader( ShaderKey );

        foreach ( ModelMeshData meshData in _modelData.Meshes )
        {
            string? textureKey = _useDefaultMaterial ? null : meshData.TextureKey ?? _defaultTextureKey;
            string? materialKey =
                _useDefaultMaterial ? meshData.MaterialKey ?? _defaultMaterialKey : meshData.MaterialKey;

            Texture? texture = textureKey != null ? textureStore.GetTexture( textureKey ) : null;
            Material? material = materialKey != null ? materialStore.GetMaterial( materialKey ) : null;

            _meshes.Add( new RenderableMesh( _gl, meshData, texture, material ) );
        }

        _isInitialized = true;
    }

    protected override void OnInit()
    {
    }

    public override void OnUpdate( double dt )
    {
    }

    public override unsafe void OnRender( double dt )
    {
        _gl.Enable( EnableCap.DepthTest );

        _shader.Use();
        _shader.SetUniform( "uModel", _transform.ModelMatrix );

        Matrix4x4.Invert( _transform.ModelMatrix, out Matrix4x4 inverseModel );
        Matrix4x4 normalMatrix = Matrix4x4.Transpose( inverseModel );
        _shader.TrySetUniform( "uNormalMatrix", normalMatrix );

        foreach ( RenderableMesh mesh in _meshes )
        {
            mesh.Bind();
            BindMeshResources( mesh );
            _gl.DrawElements( PrimitiveType.Triangles, ( uint )mesh.IndexCount, DrawElementsType.UnsignedInt, null );
        }
    }

    public override unsafe void OnRenderPicking( GL gl, Shader pickingShader )
    {
        pickingShader.Use();
        pickingShader.SetUniform( "uModel", _transform.ModelMatrix );

        foreach ( RenderableMesh mesh in _meshes )
        {
            mesh.Bind();
            gl.DrawElements( PrimitiveType.Triangles, ( uint )mesh.IndexCount, DrawElementsType.UnsignedInt, null );
        }
    }

    public override void OnClose()
    {
        foreach ( RenderableMesh mesh in _meshes )
        {
            mesh.Dispose();
        }

        _meshes.Clear();
    }

    private void BindMeshResources( RenderableMesh mesh )
    {
        if ( mesh.Texture != null )
        {
            _shader.TrySetUniform( "uTextureId", mesh.Texture.TextureId );
        }

        Material? material = mesh.Material;
        if ( material == null )
        {
            ResetMaterialUniforms();
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
        _shader.TrySetUniform( "uMaterial.hasAlbedoMap", 0 );
        _shader.TrySetUniform( "uMaterial.hasNormalMap", 0 );
        _shader.TrySetUniform( "uMaterial.hasMetallicMap", 0 );
        _shader.TrySetUniform( "uMaterial.hasRoughnessMap", 0 );
        _shader.TrySetUniform( "uMaterial.hasAoMap", 0 );
    }

    private sealed class RenderableMesh : IDisposable
    {
        private readonly BufferObject<float> _vbo;
        private readonly BufferObject<uint> _ebo;
        private readonly VertexArrayObject<float, uint> _vao;

        public RenderableMesh( GL gl, ModelMeshData data, Texture? texture, Material? material )
        {
            _vbo = new BufferObject<float>( gl, data.Vertices, BufferTargetARB.ArrayBuffer );
            _ebo = new BufferObject<uint>( gl, data.Indices, BufferTargetARB.ElementArrayBuffer );
            _vao = new VertexArrayObject<float, uint>( gl, _vbo, _ebo );

            _vao.VertexAttributePointer( 0, 3, VertexAttribPointerType.Float, 8, 0 );
            _vao.VertexAttributePointer( 1, 3, VertexAttribPointerType.Float, 8, 3 );
            _vao.VertexAttributePointer( 2, 2, VertexAttribPointerType.Float, 8, 6 );

            Texture = texture;
            Material = material;
            IndexCount = data.Indices.Length;
        }

        public Texture? Texture { get; }

        public Material? Material { get; }

        public int IndexCount { get; }

        public void Bind()
        {
            _vao.Bind();
        }

        public void Dispose()
        {
            _vbo.Dispose();
            _ebo.Dispose();
            _vao.Dispose();
        }
    }
}