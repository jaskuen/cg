using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL.Objects;

namespace Lab3.FishTank.Shapes;

public abstract class Shape : RenderableObject
{
    public Color FillColor { get; set; } = Color.White;
    public Color OutlineColor { get; set; } = Color.White;
    public bool IsFilled { get; set; } = true;
    public bool HasOutline { get; set; } = true;
    public float OutlineWidth { get; set; } = 1.0f;

    public Shape(string shaderKey) : base(shaderKey) { }

    public override unsafe void OnRender(double dt)
    {
        _shader.Use();
        _shader.SetUniform("uModel", _transform.ModelMatrix);
        _vao.Bind();

        if (IsFilled)
        {
            _gl.PolygonMode(GLEnum.FrontAndBack, PolygonMode.Fill);
            _shader.SetUniform("uColor", FillColor);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
        }

        if (HasOutline)
        {
            _gl.PolygonMode(GLEnum.FrontAndBack, PolygonMode.Line);
            _gl.LineWidth(OutlineWidth);
            _shader.SetUniform("uColor", OutlineColor);
            
            _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
        }

        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }

    public void SetPosition(Vector3 position)
    {
        _transform.Position = position;
    }

    public void SetScale(Vector3 scale)
    {
        _transform.Scale = scale;
    }

    public void SetRotation(float rotation)
    {
        _transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotation);
    }
}