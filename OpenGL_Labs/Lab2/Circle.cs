using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab2;

public class Circle : RenderableObject
{
    private Vector2 _center;
    private float _radius;

    private Color _color;

    public Circle(Vector2 center, float radius, Color color, string shaderKey, string textureKey) : base(shaderKey,
        textureKey)
    {
        _center = center;
        _radius = radius;
        _color = color;
        _transform = new Transform();
    }

    protected override void OnInit()
    {
        InitVertices();

        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        // Атрибут 0: позиция (2 floats)
        _vao.VertexAttributePointer(0, 2, VertexAttribPointerType.Float, 4, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 4, 2);

        _transform.Scale = _radius;
        _transform.Position = new Vector3(_center.X, _center.Y, 0.5f);
    }

    public override void OnUpdate(double dt)
    {
        _transform.Position = new Vector3(_transform.Position.X + 0.001f, _transform.Position.Y + 0.001f, 0);
    }

    public override void OnRender(double dt)
    {
        unsafe
        {
            // Устанавливаем uniforms
            _shader.Use();

            _shader.SetUniform("uModel", _transform.ViewMatrix);
            // _shader.SetUniform("uColor", _color);

            if (_texture != null)
            {
                _texture.Bind();

                //Setting a uniform.
                _shader.SetUniform("uTexture", 0);
            }

            // Рисуем квад
            _vao.Bind();
            _gl.Disable(EnableCap.DepthTest);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
        }
    }

    private void InitVertices()
    {
        if (_texture == null)
        {
            _vertices =
            [
                -_radius, -_radius,
                _radius, -_radius,
                -_radius, _radius,
                _radius, _radius,
            ];
            return;
        }

        _vertices =
        [
            -_radius, -_radius, 0.0f, 0.0f,
            _radius, -_radius, 1.0f, 0.0f,
            -_radius, _radius, 0.0f, 1.0f,
            _radius, _radius, 1.0f, 1.0f,
        ];

        _indices =
        [
            0, 1, 2,
            1, 2, 3
        ];
    }
}