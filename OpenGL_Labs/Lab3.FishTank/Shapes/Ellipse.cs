using Silk.NET.OpenGL;
using SilkOpenGL;

namespace Lab3.FishTank.Shapes;

public class Ellipse : Shape
{
    private int _segments;

    public Ellipse(int segments = 32) : base(Program.LineShaderName)
    {
        _segments = segments;
    }

    protected override void OnInit()
    {
        List<float> verts = new() { 0, 0, 0 };
        List<uint> inds = new();

        for (int i = 0; i <= _segments; i++)
        {
            float angle = i * MathF.PI * 2 / _segments;
            verts.AddRange(new[] { MathF.Cos(angle), MathF.Sin(angle), 0f });

            if (i > 0)
            {
                inds.Add(0);
                inds.Add((uint)i);
                inds.Add((uint)(i + 1));
            }
        }

        _vertices = verts.ToArray();
        _indices = inds.ToArray();

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