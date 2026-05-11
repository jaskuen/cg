using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;
using SilkOpenGL.Store;

namespace Lab7.ShaderPrograms.Tasks;

public class Task4 : RenderableObject, IClickable
{
    private const int VertexStride = 5;
    private const float Width = 2.6f;
    private const float Height = 1.7f;
    private const float TransitionDuration = 2.4f;

    private readonly string _sourceTextureKey;
    private readonly string _targetTextureKey;

    private SilkOpenGL.Texture _sourceTexture = null!;
    private SilkOpenGL.Texture _targetTexture = null!;

    private bool _isTransitioning;
    private bool _showingTargetTexture;
    private bool _transitionFromTargetTexture;
    private float _progress;
    private Vector2 _origin = new( 0.5f, 0.5f );

    public uint ColorId { get; set; }

    public Task4( string sourceTextureKey, string targetTextureKey ) : base( Program.RippleShaderName )
    {
        _sourceTextureKey = sourceTextureKey;
        _targetTextureKey = targetTextureKey;
    }

    public override void Init( ShaderStore shaderStore, TextureStore textureStore, MaterialStore materialStore, GL gl )
    {
        base.Init( shaderStore, textureStore, materialStore, gl );
        _sourceTexture = textureStore.GetTexture( _sourceTextureKey );
        _targetTexture = textureStore.GetTexture( _targetTextureKey );
    }

    protected override void OnInit()
    {
        InitQuad();

        _vbo = new BufferObject<float>( _gl, _vertices, BufferTargetARB.ArrayBuffer );
        _ebo = new BufferObject<uint>( _gl, _indices, BufferTargetARB.ElementArrayBuffer );
        _vao = new VertexArrayObject<float, uint>( _gl, _vbo, _ebo );

        _vao.VertexAttributePointer( 0, 3, VertexAttribPointerType.Float, VertexStride, 0 );
        _vao.VertexAttributePointer( 1, 2, VertexAttribPointerType.Float, VertexStride, 3 );
    }

    public override void OnUpdate( double dt )
    {
        if ( !_isTransitioning )
        {
            return;
        }

        _progress += ( float )dt / TransitionDuration;
        if ( _progress >= 1f )
        {
            _progress = 1f;
            _isTransitioning = false;
        }
    }

    public override unsafe void OnRender( double dt )
    {
        _gl.Disable( EnableCap.CullFace );

        _sourceTexture.Bind( TextureUnit.Texture0 );
        _targetTexture.Bind( TextureUnit.Texture1 );

        _shader.Use();
        _shader.SetUniform( "uModel", _transform.ModelMatrix );
        _shader.SetUniform( "uSourceImage", 0 );
        _shader.SetUniform( "uTargetImage", 1 );
        _shader.SetUniform( "uProgress", _progress );
        _shader.SetUniform( "uOrigin", new Vector3( _origin, 0f ) );
        _shader.SetUniform( "uFromTarget", _transitionFromTargetTexture ? 1 : 0 );

        _vao.Bind();
        _gl.DrawElements( PrimitiveType.Triangles, ( uint )_indices.Length, DrawElementsType.UnsignedInt, null );
    }

    public void OnMouseDown( Vector3 position )
    {
    }

    public void OnMouseUp( Vector3 position )
    {
        if ( _isTransitioning )
        {
            return;
        }

        _origin = WorldToUv( position );
        _transitionFromTargetTexture = _showingTargetTexture;
        _showingTargetTexture = !_showingTargetTexture;
        _progress = 0f;
        _isTransitioning = true;
    }

    public void OnMouseMove( Vector3 position )
    {
    }

    public void OnMouseEnter()
    {
    }

    public void OnMouseLeave()
    {
    }

    private static Vector2 WorldToUv( Vector3 worldPosition )
    {
        float u = Math.Clamp( worldPosition.X / Width + 0.5f, 0f, 1f );
        float v = Math.Clamp( worldPosition.Y / Height + 0.5f, 0f, 1f );

        return new Vector2( u, v );
    }

    private void InitQuad()
    {
        float halfWidth = Width / 2f;
        float halfHeight = Height / 2f;

        _vertices =
        [
            -halfWidth, -halfHeight, 0f, 0f, 0f,
            halfWidth, -halfHeight, 0f, 1f, 0f,
            halfWidth, halfHeight, 0f, 1f, 1f,
            -halfWidth, halfHeight, 0f, 0f, 1f
        ];

        _indices =
        [
            0, 1, 2,
            0, 2, 3
        ];
    }
}
