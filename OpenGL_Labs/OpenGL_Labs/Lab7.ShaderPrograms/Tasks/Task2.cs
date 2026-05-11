using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab7.ShaderPrograms.Tasks;

public class Task2 : RenderableObject
{
    private const int VertexStride = 7;
    private const float FlagWidth = 2.4f;
    private const float FlagHeight = 1.6f;
    private const float GridUnit = FlagWidth / 30f;

    public Task2() : base( Program.ChinaFlagShaderName )
    {
    }

    protected override void OnInit()
    {
        Init();

        _vbo = new BufferObject<float>( _gl, _vertices, BufferTargetARB.ArrayBuffer );
        _ebo = new BufferObject<uint>( _gl, _indices, BufferTargetARB.ElementArrayBuffer );
        _vao = new VertexArrayObject<float, uint>( _gl, _vbo, _ebo );

        _vao.VertexAttributePointer( 0, 3, VertexAttribPointerType.Float, VertexStride, 0 );
        _vao.VertexAttributePointer( 1, 2, VertexAttribPointerType.Float, VertexStride, 3 );
        _vao.VertexAttributePointer( 2, 1, VertexAttribPointerType.Float, VertexStride, 5 );
        _vao.VertexAttributePointer( 3, 1, VertexAttribPointerType.Float, VertexStride, 6 );
    }

    public override void OnUpdate( double dt )
    {
    }

    public override unsafe void OnRender( double dt )
    {
        _gl.Disable( EnableCap.CullFace );

        _shader.Use();
        _shader.SetUniform( "uModel", _transform.ModelMatrix );

        _vao.Bind();
        _gl.DrawElements( PrimitiveType.Points, ( uint )_indices.Length, DrawElementsType.UnsignedInt, null );
    }

    private void Init()
    {
        List<float> vertices = [];
        List<uint> indices = [];

        AddPoint( vertices, indices, Vector3.Zero, new Vector2( FlagWidth, FlagHeight ), 0f, 0f );

        Vector2 bigStarCenter = GridToWorld( 5f, 5f );
        AddStarPoint( vertices, indices, bigStarCenter, 3f * GridUnit, MathF.PI / 2f );

        AddSmallStarPoint( vertices, indices, bigStarCenter, GridToWorld( 10f, 2f ) );
        AddSmallStarPoint( vertices, indices, bigStarCenter, GridToWorld( 12f, 4f ) );
        AddSmallStarPoint( vertices, indices, bigStarCenter, GridToWorld( 12f, 7f ) );
        AddSmallStarPoint( vertices, indices, bigStarCenter, GridToWorld( 10f, 9f ) );

        _vertices = [.. vertices];
        _indices = [.. indices];
    }

    private static Vector2 GridToWorld( float x, float y )
    {
        float left = -FlagWidth / 2f;
        float top = FlagHeight / 2f;

        return new Vector2( left + x * GridUnit, top - y * GridUnit );
    }

    private static void AddSmallStarPoint( List<float> vertices, List<uint> indices, Vector2 bigStarCenter,
        Vector2 smallStarCenter )
    {
        Vector2 direction = bigStarCenter - smallStarCenter;
        float rotation = MathF.Atan2( direction.Y, direction.X );

        AddStarPoint( vertices, indices, smallStarCenter, GridUnit, rotation );
    }

    private static void AddStarPoint( List<float> vertices, List<uint> indices, Vector2 center, float radius,
        float rotation )
    {
        AddPoint( vertices, indices, new Vector3( center, 0.01f ), new Vector2( radius, radius ), rotation, 1f );
    }

    private static void AddPoint( List<float> vertices, List<uint> indices, Vector3 position, Vector2 size,
        float rotation, float kind )
    {
        vertices.AddRange(
        [
            position.X, position.Y, position.Z,
            size.X, size.Y,
            rotation,
            kind
        ] );

        indices.Add( ( uint )indices.Count );
    }
}
