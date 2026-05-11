using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab7.ShaderPrograms.Tasks;

public class Task3 : RenderableObject, IClickable
{
    private const int USegments = 160;
    private const int VSegments = 160;
    private const int VertexStride = 3;
    private const float DomainSize = 2.4f;
    private const float MorphSpeed = 1.2f;

    private float _morph;
    private float _targetMorph;

    public uint ColorId { get; set; }

    public Task3() : base( Program.MorphShaderName )
    {
    }

    protected override void OnInit()
    {
        Init();

        _vbo = new BufferObject<float>( _gl, _vertices, BufferTargetARB.ArrayBuffer );
        _ebo = new BufferObject<uint>( _gl, _indices, BufferTargetARB.ElementArrayBuffer );
        _vao = new VertexArrayObject<float, uint>( _gl, _vbo, _ebo );

        _vao.VertexAttributePointer( 0, 3, VertexAttribPointerType.Float, VertexStride, 0 );
    }

    public override void OnUpdate( double dt )
    {
        float delta = MorphSpeed * ( float )dt;

        if ( _morph < _targetMorph )
        {
            _morph = MathF.Min( _morph + delta, _targetMorph );
        }
        else if ( _morph > _targetMorph )
        {
            _morph = MathF.Max( _morph - delta, _targetMorph );
        }
    }

    public override unsafe void OnRender( double dt )
    {
        _gl.Enable( EnableCap.DepthTest );
        _gl.DepthMask( true );
        _gl.Disable( EnableCap.CullFace );

        _shader.Use();
        _shader.SetUniform( "uModel", _transform.ModelMatrix );
        _shader.SetUniform( "uMorph", _morph );
        _shader.SetUniform( "uBaseColor", new Vector3( 0.92f, 0.54f, 0.18f ) );

        if ( Matrix4x4.Invert( _transform.ModelMatrix, out Matrix4x4 invModel ) )
        {
            _shader.SetUniform( "uNormalMatrix", Matrix4x4.Transpose( invModel ) );
        }

        _vao.Bind();
        _gl.DrawElements( PrimitiveType.Triangles, ( uint )_indices.Length, DrawElementsType.UnsignedInt, null );
    }

    public void OnMouseDown( Vector3 position )
    {
    }

    public void OnMouseUp( Vector3 position )
    {
        _targetMorph = _targetMorph < 0.5f ? 1f : 0f;
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

    private void Init()
    {
        List<float> vertices = [];
        List<uint> indices = [];

        for ( int u = 0; u <= USegments; u++ )
        {
            float u01 = u / ( float )USegments;

            for ( int v = 0; v <= VSegments; v++ )
            {
                float v01 = v / ( float )VSegments;
                float x = ( u01 - 0.5f ) * DomainSize;
                float y = ( v01 - 0.5f ) * DomainSize;

                vertices.AddRange( [x, y, 0f] );
            }
        }

        int row = VSegments + 1;
        for ( int u = 0; u < USegments; u++ )
        {
            for ( int v = 0; v < VSegments; v++ )
            {
                uint p1 = ( uint )( u * row + v );
                uint p2 = ( uint )( ( u + 1 ) * row + v );
                uint p3 = ( uint )( ( u + 1 ) * row + v + 1 );
                uint p4 = ( uint )( u * row + v + 1 );

                indices.AddRange( [p1, p2, p3, p1, p3, p4] );
            }
        }

        _vertices = [.. vertices];
        _indices = [.. indices];
    }
}
