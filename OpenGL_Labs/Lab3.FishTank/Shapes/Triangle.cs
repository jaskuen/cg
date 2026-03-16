using Silk.NET.OpenGL;
using SilkOpenGL;

namespace Lab3.FishTank.Shapes;

public class Triangle : Shape
{
    public Triangle() : base(Program.LineShaderName)
    {
    }

    protected override void OnInit()
    {
        _vertices = new float[]
        {
            0.0f, 0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f
        };

        _indices = new uint[]
        {
            0, 1, 2
        };

        SetupBuffers();
    }

    private void SetupBuffers()
    {
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);

        _gl.BindVertexArray(0);
    }

    public override void OnUpdate(double dt)
    {
    }
}