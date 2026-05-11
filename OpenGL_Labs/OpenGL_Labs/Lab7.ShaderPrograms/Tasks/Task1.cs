using System.Drawing;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab7.ShaderPrograms.Tasks;

public class Task1 : RenderableObject
{
    private const int Segments = 2000;
    private const float Step = MathF.PI / 1000f;

    public Task1() : base( Program.CanabolaShaderName )
    {
    }

    protected override void OnInit()
    {
        Init();

        _vbo = new BufferObject<float>( _gl, _vertices, BufferTargetARB.ArrayBuffer );
        _ebo = new BufferObject<uint>( _gl, _indices, BufferTargetARB.ElementArrayBuffer );
        _vao = new VertexArrayObject<float, uint>( _gl, _vbo, _ebo );

        _vao.VertexAttributePointer( 0, 3, VertexAttribPointerType.Float, 3, 0 );
    }

    public override void OnUpdate( double dt )
    {
    }

    public override unsafe void OnRender( double dt )
    {
        _shader.Use();
        _shader.SetUniform( "uModel", _transform.ModelMatrix );
        _shader.SetUniform( "uColor", Color.FromArgb( 255, 255, 210, 80 ) );
        _shader.SetUniform( "uScale", 0.36f );

        _vao.Bind();
        _gl.LineWidth( 2.5f );
        _gl.DrawElements( PrimitiveType.LineStrip, ( uint )_indices.Length, DrawElementsType.UnsignedInt, null );
    }

    private void Init()
    {
        List<float> vertices = [];
        List<uint> indices = [];

        for ( int i = 0; i <= Segments; i++ )
        {
            float x = i * Step;
            vertices.AddRange( [x, 0f, 0f] );
            indices.Add( ( uint )i );
        }

        _vertices = [.. vertices];
        _indices = [.. indices];
    }
}
