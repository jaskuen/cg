using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Bhop;

public class Tile : RenderableObject
{
    private float _scale = 1f;
    private float _height = 0.1f;

    private Random _random;

    private static List<Color> _colors =
    [
        Color.GreenYellow,
        Color.Aquamarine,
        Color.IndianRed,
    ];

    private Color _color;

    public Tile(Vector3 position, string shaderKey) : base(shaderKey)
    {
        _transform.Position = position;
        _random = new Random();
    }

    public override void OnUpdate(double dt)
    {
    }

    protected override void OnInit()
    {
        _color = _colors[_random.Next(_colors.Count)];
        
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
            0, 3, 2,
        ];
    }
}