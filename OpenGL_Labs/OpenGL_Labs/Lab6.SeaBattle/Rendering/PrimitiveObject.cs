using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab6.SeaBattle.Rendering;

public sealed class PrimitiveObject : RenderableObject
{
    private readonly PrimitiveMesh _mesh;

    public PrimitiveObject(
        string shaderKey,
        PrimitiveMesh mesh,
        Vector3 color,
        float roughness = 0.5f,
        float metallic = 0f,
        float alpha = 1f)
        : base(shaderKey)
    {
        _mesh = mesh;
        Color = color;
        Roughness = roughness;
        Metallic = metallic;
        Alpha = alpha;
    }

    public Transform Transform => _transform;
    public Vector3 Color { get; set; }
    public float Roughness { get; set; }
    public float Metallic { get; set; }
    public float Alpha { get; set; }

    protected override void OnInit()
    {
        _vertices = _mesh.Vertices;
        _indices = _mesh.Indices;
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 8, 3);
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, 8, 6);
    }

    public override void OnUpdate(double dt)
    {
    }

    public override unsafe void OnRender(double dt)
    {
        _vao.Bind();
        _ebo.Bind();
        _vbo.Bind();
        _shader.SetUniform("uModel", _transform.ModelMatrix);
        _shader.TrySetUniform("uColor", Color);
        _shader.TrySetUniform("uMaterial.baseColor", Color);
        _shader.TrySetUniform("uRoughness", Roughness);
        _shader.TrySetUniform("uMetallic", Metallic);
        _shader.TrySetUniform("uAlpha", Alpha);

        if (Matrix4x4.Invert(_transform.ModelMatrix, out Matrix4x4 invModel))
        {
            _shader.TrySetUniform("uNormalMatrix", Matrix4x4.Transpose(invModel));
        }
        else
        {
            _shader.TrySetUniform("uNormalMatrix", Matrix4x4.Identity);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }
}
