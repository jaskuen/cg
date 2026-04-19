using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL.Objects;

namespace SilkOpenGL.Model;

public class RenderableMesh : RenderableObject
{
    private ModelMeshData _data;
    private readonly RenderableObject _parent;

    private bool _firstRender = true;
    private DateTime _renderTime = DateTime.Now;

    public RenderableMesh(RenderableObject parent, ModelMeshData data, string shaderKey, string textureKey,
        bool isMaterial) : base(
        shaderKey, textureKey, isMaterial)
    {
        _parent = parent;
        _data = data;
    }

    public Texture? Texture { get; }

    public Material? Material { get; }

    public int IndexCount { get; }

    public GLEnum DrawPrimitive { get; }

    public override unsafe void OnRender(double dt)
    {
        // if (_firstRender)
        // {
        //     _renderTime = DateTime.Now.AddSeconds(_vao.Handle);
        //     _firstRender = false;
        // }
        //
        // if (_renderTime.AddSeconds(1) < DateTime.Now)
        // {
        //     _renderTime = DateTime.Now.AddSeconds(18);
        // }
        //
        // if (_renderTime > DateTime.Now)
        // {
        //     return;
        // }

        // Console.WriteLine($"Rendering {_data.Name}");

        _vao.Bind();
        _gl.Enable(EnableCap.DepthTest);
        _gl.FrontFace(FrontFaceDirection.CW);
        _gl.DepthMask(true);

        _gl.Disable(EnableCap.CullFace);

        _shader.SetUniform("uModel", _parent._transform.ModelMatrix);

        if (Matrix4x4.Invert(_parent._transform.ModelMatrix, out var invModel))
        {
            Matrix4x4 normalMatrix = Matrix4x4.Transpose(invModel);
            _shader.SetUniform("uNormalMatrix", normalMatrix);
        }

        if (_data.DrawPrimitive != GLEnum.Triangles)
        {
            Console.WriteLine($"Using VAO {_vao.Handle}, VBO: {_vbo.Handle}, EBO: {_ebo.Handle}");
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

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 8, 3);
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, 8, 6);
    }

    public override void OnUpdate(double dt)
    {
    }

    public uint ColorId { get; set; }
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