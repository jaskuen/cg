using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab2;

public class Tile : RenderableObject
{
    private const float Size = 0.05f;

    private Vector2D<float> _position;
    private float _deltaScale = 0.05f;

    public Tile(Vector2D<float> coords, string shaderKey) : base(shaderKey)
    {
        _position = coords;
        _transform = new Transform();
        _transform.Position = new Vector3(coords.X, coords.Y, 0f);
    }

    protected override void OnInit()
    {
        InitVertices();

        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        //Telling the VAO object how to lay out the attribute pointers
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
    }

    public override void OnUpdate(double delta)
    {
        Transform(new Vector2D<float>(0.001f, 0.001f));
    }

    public override unsafe void OnRender(double delta)
    {
        //Binding and using our VAO and shader.
        _vao.Bind();
        _shader.Use();

        // Передача матрицы модели
        _shader.SetUniform("uModel", _transform.ViewMatrix);
        
        if (_texture != null)
        {
            _texture.Bind();

            //Setting a uniform.
            _shader.SetUniform("uTexture", 0);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    public void Transform(Vector2D<float> delta)
    {
        _transform.Scale += _deltaScale;
        if (Math.Abs(_transform.Scale) > 2f)
        {
            _deltaScale *= -1;
        }
    }

    private void InitVertices()
    {
        float half = Size / 2f;


        if (_texture == null)
        {
            _vertices =
            [
                -half, -half, 0.0f,
                half, -half, 0.0f,
                -half, half, 0.0f,
                half, half, 0.0f
            ];
            _indices =
            [
                0, 1, 2,
                1, 2, 3
            ];
        }
    }
}