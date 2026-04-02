using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab4.Labyrinth;

public class Floor : RenderableObject
{
    private Color _color = Color.Chocolate;

    public Floor(float scale, string shaderKey) : base(shaderKey)
    {
        _transform.Scale = new Vector3(scale);
        _transform.Position += new Vector3(0f, -0.5f, 0f);
    }

    public override void OnUpdate(double dt)
    {
    }

    protected override void OnInit()
    {
        InitVertices();

        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
    }

    public override unsafe void OnRender(double dt)
    {
        _vao.Bind();
        _shader.Use();

        _shader.SetUniform("uModel", _transform.ModelMatrix);

        _shader.SetUniform("uColor", _color);
        _shader.TrySetUniform("uHasTexture", 0);

        _gl.DrawElements(GLEnum.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private void InitVertices()
    {
        _vertices =
        [
            -1f, 0f, -1f, 0f, 0f,
            -1f, 0f, 1f, 0f, 1f,
            1f, 0f, -1f, 1f, 0f,
            1f, 0f, 1f, 1f, 1f,
        ];

        _indices =
        [
            0, 1, 3,
            0, 3, 2
        ];
    }
}