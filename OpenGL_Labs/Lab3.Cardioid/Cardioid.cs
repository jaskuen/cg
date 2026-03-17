using System.Drawing;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab3.Cardioid;

public class Cardioid : RenderableObject
{
    private float _radius;
    private int _verticesCount;

    public Cardioid(float radius, int verticesCount = 100) : base(Program.LineShaderName)
    {
        _radius = radius;
        _verticesCount = verticesCount;
    }

    protected override void OnInit()
    {
        InitVertices();

        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
    }

    public override void OnUpdate(double dt)
    {
    }

    public override unsafe void OnRender(double dt)
    {
        _shader.Use();
        
        _shader.SetUniform("uModel", _transform.ModelMatrix);
        _shader.SetUniform("uColor", Color.Black);
        
        _vao.Bind();
        _gl.DrawElements(PrimitiveType.LineLoop, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private void InitVertices()
    {
        float angle = 0;
        float delta = 2 * (float)Math.PI / _verticesCount;

        List<float> vertices = new List<float>();
        List<uint> indices = new List<uint>();

        for (int i = 0; i < _verticesCount; i++)
        {
            vertices.AddRange([CountX(angle), CountY(angle), 0]);
            angle += delta;
            indices.Add((uint)i);
        }

        _vertices = vertices.ToArray();
        _indices = indices.ToArray();
    }

    private float CountX(float t)
    {
        return 2 * _radius * MathF.Cos(t) + _radius * MathF.Cos(2 * t);
    }

    private float CountY(float t)
    {
        return 2 * _radius * MathF.Sin(t) + _radius * MathF.Sin(2 * t);
    }
}