using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL.Objects;

namespace SilkOpenGL.Lighting;

public class LightObject : RenderableObject
{
    public const string LightObjectShader = "LightObjectShader";
    
    public LightObject(Vector3 pos) : base(LightObjectShader)
    {
        _transform.Position = pos;
    }

    public override void OnUpdate(double dt)
    {
    }

    public override unsafe void OnRender(double dt)
    {
        _vao.Bind();
        _shader.Use();

        _shader.SetUniform("uModel", _transform.ModelMatrix);

        _gl.DrawElements(GLEnum.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    protected override void OnInit()
    {
        InitVertices();

        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
    }

    private void InitVertices()
    {
        _vertices =
        [
            -0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, 0.5f,
            -0.5f, -0.5f, 0.5f,
            -0.5f, 0.5f, 0.5f,
            0.5f, 0.5f, 0.5f,
            0.5f, 0.5f, -0.5f,
            -0.5f, 0.5f, -0.5f,
        ];

        _indices =
        [
            0, 1, 2,
            0, 2, 3,
            0, 1, 6,
            0, 6, 7,
            0, 3, 4,
            0, 4, 7,
            1, 2, 5,
            1, 5, 6,
            2, 3, 4,
            2, 4, 5,
            4, 5, 6,
            4, 6, 7,
        ];
    }
}