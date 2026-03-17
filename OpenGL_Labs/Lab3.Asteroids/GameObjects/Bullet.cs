using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Helpers;
using SilkOpenGL.Objects;

namespace Lab3.Asteroids.GameObjects;

public class Bullet : RenderableObject
{
    private Vector3 _direction;
    private float _speed = 2.0f;
    private float _lifeTime = 1.2f; // Пуля живет 1.2 секунды
    private const float Scale = 0.02f;

    public bool IsDead { get; set; }

    public Bullet(Vector3 pos, Vector3 direction, float rotation)
        : base(Program.LineShaderName)
    {
        _direction = Vector3.Normalize(direction);

        _transform.Position = pos;
        _transform.Scale = new Vector3(0.015f);
        _transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(rotation));
    }

    protected override void OnInit()
    {
        InitVertices();

        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);

        _transform.Scale = new Vector3(Scale);
    }

    public override void OnUpdate(double dt)
    {
        // 1. Движение
        _transform.Position += _direction * _speed * (float)dt;

        _lifeTime -= (float)dt;
        if (_lifeTime <= 0)
        {
            IsDead = true;
        }

        WrapAround();
    }

    private void WrapAround()
    {
        Vector3 pos = _transform.Position;
        if (pos.X > 2f) pos.X = 2f; else if (pos.X < -2f) pos.X = 2f;
        if (pos.Y > 1.3f) pos.Y = -1.3f; else if (pos.Y < -1.3f) pos.Y = 1.3f;
        _transform.Position = pos;
    }

    public override unsafe void OnRender(double dt)
    {
        _shader.Use();
        _shader.SetUniform("uModel", _transform.ModelMatrix);
        _shader.SetUniform("uColor", Color.White);

        _vao.Bind();

        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private void InitVertices()
    {
        _indices =
        [
            0, 1, 2,
            2, 1, 3
        ];

        _vertices =
        [
            -0.3f, -1f, 0f,
            0.3f, -1f, 0f,
            -0.3f, 1f, 0f,
            0.3f, 1f, 0f
        ];
    }
}