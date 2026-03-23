using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab2;

public class Tile : RenderableObject, IClickable
{
    private float _size;

    private bool _hovered = false;
    private Color _color;

    private Circle? _circle;
    private bool _active;

    public Circle? Circle => _circle;
    public Color? BallColor => _circle?.Color ?? null;

    public Action HandleClick { get; set; }

    public Tile(Vector3 coords, float size, Color color, string shaderKey, string textureKey) : base(shaderKey,
        textureKey)
    {
        _size = size;
        _transform = new Transform
        {
            Position = coords
        };

        _color = color;
    }

    protected override void OnInit()
    {
        InitVertices();

        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 2, VertexAttribPointerType.Float, 4, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 4, 2);
    }

    public override void OnUpdate(double delta)
    {
    }

    public override unsafe void OnRender(double delta)
    {
        _vao.Bind();
        _shader.Use();

        // Передача матрицы модели
        _shader.SetUniform("uModel", _transform.ModelMatrix);

        _shader.SetUniform("uColor", _hovered
            ? Color.Crimson
            : _active
                ? Color.HotPink
                : _color);

        if (_texture != null)
        {
            _texture.Bind();

            _shader.SetUniform("uTexture", 0);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private void InitVertices()
    {
        float half = _size / 2f;

        _indices =
        [
            0, 1, 2,
            1, 2, 3
        ];

        if (_texture == null)
        {
            _vertices =
            [
                -half, -half, 0.0f,
                half, -half, 0.0f,
                -half, half, 0.0f,
                half, half, 0.0f
            ];

            return;
        }

        _vertices =
        [
            -half, -half, 0.0f, 0.0f,
            half, -half, 1.0f, 0.0f,
            -half, half, 0.0f, 1.0f,
            half, half, 1.0f, 1.0f,
        ];
    }

    public uint ColorId { get; set; }

    public void SetActive(bool active) => _active = active;

    public void OnMouseDown(Vector3 position)
    {
        HandleClick?.Invoke();
    }

    public void OnMouseUp(Vector3 position)
    {
    }

    public void OnMouseMove(Vector3 position)
    {
    }

    public void OnMouseEnter()
    {
        if (_circle is null)
        {
            _hovered = true;
        }
    }

    public void OnMouseLeave()
    {
        _hovered = false;
    }

    public void PlaceCircle(Circle circle)
    {
        _circle = circle;
    }

    public Circle RemoveCircle()
    {
        Circle circle = _circle!;
        _circle = null;
        return circle;
    }
}