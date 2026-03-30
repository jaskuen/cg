using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab5.Tiles;

/// <summary>
/// A thin 3D tile for a memory game:
/// - When <see cref="Reveal"/> was called, the tile shows the provided image texture.
/// - When <see cref="Hide"/> was called, the tile shows a solid back color (no image).
/// </summary>
public class Tile : RenderableObject, IClickable
{
    private readonly float _width;
    private readonly float _height;
    private readonly float _depth;
    private readonly Color _backColor;

    private bool _hovered;
    private bool _isFaceUp;
    private bool _isLocked;
    private float _targetAngle;
    private float _currentAngle;
    private bool _animationFinished;

    public event Action? Clicked;

    public int TextureId => _texture?.TextureId ?? 0;
    public bool AnimationFinished => _animationFinished;

    public Tile(
        string shaderKey,
        string textureKey,
        Vector3 position,
        float width = 1f,
        float height = 1f,
        float depth = 0.12f,
        Color? backColor = null,
        bool isFaceUp = false
    ) : base(shaderKey, textureKey)
    {
        _width = width;
        _height = height;
        _depth = depth;
        _backColor = backColor ?? Color.SlateGray;

        _transform.Position = position;
        _transform.Scale = Vector3.One;

        _isFaceUp = isFaceUp;
        _currentAngle = 0;
    }

    public bool IsFaceUp => _isFaceUp;
    public bool IsLocked => _isLocked;

    public void Lock() => _isLocked = true;
    public void Unlock() => _isLocked = false;

    public void Reveal()
    {
        if (_isFaceUp) return;
        _isFaceUp = true;
        _isLocked = true;
        SetRotationForFace(true);
    }

    public void Hide()
    {
        if (!_isFaceUp && !_isLocked) return;
        _isFaceUp = false;
        _isLocked = false;
        SetRotationForFace(false);
    }

    private void SetRotationForFace(bool faceUp)
    {
        // FaceUp: 0° around Y. FaceDown: 180° around Y.
        _targetAngle = DegreesToRadians(faceUp ? 180f : 0f);
        _animationFinished = false;
    }

    protected override void OnInit()
    {
        InitVertices();

        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 6, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 6, 3);
    }

    public override void OnUpdate(double dt)
    {
        float speed = (float)dt * 2.5f;

        if (Math.Abs(_targetAngle - _currentAngle) < speed)
        {
            _transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, _targetAngle);
            _currentAngle = _targetAngle;
            _animationFinished = true;
            return;
        }

        _currentAngle += speed;

        if (_currentAngle >= 2 * Math.PI)
        {
            _currentAngle = 0;
        }

        _transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, _currentAngle);
    }

    public override unsafe void OnRender(double dt)
    {
        _gl.Enable(EnableCap.DepthTest);

        _vao.Bind();
        _shader.Use();
        _shader.SetUniform("uModel", _transform.ModelMatrix);

        bool textureEnabled = _isFaceUp || (!_isFaceUp && !_animationFinished);

        Color baseColor = _hovered ? Color.OrangeRed : _backColor;

        _shader.SetUniform("uColor", baseColor);
        _shader.TrySetUniform("uHasTexture", textureEnabled ? 1 : 0);

        if (textureEnabled && _texture != null)
        {
            _texture.Bind();
            _shader.SetUniform("uHandle", _texture.TextureId);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
        _shader.SetUniform("uHandle", 0);
    }

    private void InitVertices()
    {
        float hw = _width / 2f;
        float hh = _height / 2f;
        float hd = _depth / 2f;

        List<float> vertices = [];
        List<uint> indices = [];

        void AddVertex(float x, float y, float z, float u, float v, bool isTexture)
        {
            vertices.Add(x);
            vertices.Add(y);
            vertices.Add(z);
            vertices.Add(u);
            vertices.Add(v);
            vertices.Add(isTexture ? 1f : 0f);
        }

        void AddFace(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, bool isTexture = false)
        {
            uint start = (uint)(vertices.Count / 6);
            AddVertex(v0.X, v0.Y, v0.Z, 0f, 0f, isTexture);
            AddVertex(v1.X, v1.Y, v1.Z, 1f, 0f, isTexture);
            AddVertex(v2.X, v2.Y, v2.Z, 0f, 1f, isTexture);
            AddVertex(v3.X, v3.Y, v3.Z, 1f, 1f, isTexture);

            // Two triangles for the quad.
            indices.Add(start + 0);
            indices.Add(start + 1);
            indices.Add(start + 2);

            indices.Add(start + 1);
            indices.Add(start + 3);
            indices.Add(start + 2);
        }

        // Top
        AddFace(
            new Vector3(-hw, +hd, -hh),
            new Vector3(+hw, +hd, -hh),
            new Vector3(-hw, +hd, +hh),
            new Vector3(+hw, +hd, +hh)
        );

        // Bottom
        AddFace(
            new Vector3(+hw, -hd, -hh),
            new Vector3(-hw, -hd, -hh),
            new Vector3(+hw, -hd, +hh),
            new Vector3(-hw, -hd, +hh),
            true
        );

        // Sides
        AddFace(
            new Vector3(-hw, +hd, -hh),
            new Vector3(-hw, -hd, -hh),
            new Vector3(-hw, +hd, +hh),
            new Vector3(-hw, -hd, +hh)
        );

        AddFace(
            new Vector3(+hw, +hd, -hh),
            new Vector3(+hw, -hd, -hh),
            new Vector3(+hw, +hd, +hh),
            new Vector3(+hw, -hd, +hh)
        );

        AddFace(
            new Vector3(-hw, -hd, -hh),
            new Vector3(+hw, -hd, -hh),
            new Vector3(-hw, +hd, -hh),
            new Vector3(+hw, +hd, -hh)
        );

        AddFace(
            new Vector3(-hw, +hd, +hh),
            new Vector3(+hw, +hd, +hh),
            new Vector3(-hw, -hd, +hh),
            new Vector3(+hw, -hd, +hh)
        );

        _vertices = vertices.ToArray();
        _indices = indices.ToArray();
    }

    private static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);

    public uint ColorId { get; set; }

    public void OnMouseDown(Vector3 position)
    {
        Clicked?.Invoke();

        if (_isLocked) return;
        if (_isFaceUp)
        {
            Hide();
        }
        else
        {
            Reveal();
        }
    }

    public void OnMouseUp(Vector3 position)
    {
    }

    public void OnMouseMove(Vector3 position)
    {
    }

    public void OnMouseEnter()
    {
        _hovered = true;
    }

    public void OnMouseLeave()
    {
        _hovered = false;
    }

    public void SetScale(float scale)
    {
        _transform.Scale = new Vector3(scale);
    }
}