using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL.Objects;

namespace SilkOpenGL.Model;

public class RenderableMesh : RenderableObject
{
    private readonly ModelMeshData _data;
    private readonly RenderableObject _parent;
    private readonly bool _useImportedDiffuseColor;

    public RenderableMesh(RenderableObject parent, ModelMeshData data, string shaderKey, string? textureKey,
        bool isMaterial) : base(
        shaderKey, textureKey, isMaterial)
    {
        _parent = parent;
        _data = data;
        _useImportedDiffuseColor = !isMaterial;
    }

    public Texture? Texture => _texture;

    public Material? Material => _material;

    public int IndexCount => _data.Indices.Length;

    public PrimitiveType DrawPrimitive => _data.DrawPrimitive;

    protected override Matrix4x4 WorldModelMatrix => _data.LocalTransform * _parent._transform.ModelMatrix;

    public override unsafe void OnRender(double dt)
    {
        _vao.Bind();
        if (_indices.Length == 0)
        {
            return;
        }

        Matrix4x4 meshModel = WorldModelMatrix;
        _shader.SetUniform("uModel", meshModel);
        if (_useImportedDiffuseColor)
        {
            _shader.TrySetUniform("uMaterial.baseColor", _data.DiffuseColor);
        }

        if (Matrix4x4.Invert(meshModel, out var invModel))
        {
            Matrix4x4 normalMatrix = Matrix4x4.Transpose(invModel);
            _shader.TrySetUniform("uNormalMatrix", normalMatrix);
        }

        _gl.DrawElements(_data.DrawPrimitive, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    protected override void OnInit()
    {
        _vertices = _data.Vertices;
        _indices = _data.Indices;

        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        uint stride = (uint)_data.VertexStride;
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, stride, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, stride, 3);
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, stride, 6);
        _vao.VertexAttributePointer(3, 3, VertexAttribPointerType.Float, stride, 8);
        _vao.VertexAttributePointer(4, 3, VertexAttribPointerType.Float, stride, 11);
    }

    public override void OnUpdate(double dt)
    {
    }

    public uint ColorId { get; set; }

    public override unsafe void OnRenderPicking(GL gl, Shader pickingShader)
    {
        _vao.Bind();
        if (_indices.Length == 0)
        {
            return;
        }

        Matrix4x4 meshModel = WorldModelMatrix;
        pickingShader.SetUniform("uModel", meshModel);
        gl.DrawElements(_data.DrawPrimitive, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    public void OnMouseDown(Vector3 position)
    {
        
    }

    public void OnMouseUp(Vector3 position)
    {
    }

    public void OnMouseMove(Vector3 position)
    {
        Console.WriteLine($"Mesh {_vao.Handle}, {_data.Name}");
    }

    public void OnMouseEnter()
    {
    }

    public void OnMouseLeave()
    {
    }
}
