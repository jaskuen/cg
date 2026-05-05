using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab6.SeaBattle.Rendering;

public sealed class Sky : RenderableObject
{
    private uint _skyTexture;

    public Sky(string shaderKey) : base(shaderKey)
    {
        _transform.Position = new Vector3(0f, 6.5f, -36f);
    }

    protected override unsafe void OnInit()
    {
        PrimitiveMesh mesh = MeshFactory.VerticalPlane(45f, 24f);
        _vertices = mesh.Vertices;
        _indices = mesh.Indices;
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 8, 3);
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, 8, 6);

        byte[] pixels = BuildSkyTexture(128, 128);
        _skyTexture = _gl.GenTexture();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _skyTexture);
        fixed (byte* ptr = pixels)
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, 128, 128, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
    }

    public override void OnUpdate(double dt)
    {
    }

    public override unsafe void OnRender(double dt)
    {
        _gl.Disable(EnableCap.DepthTest);
        _vao.Bind();
        _ebo.Bind();
        _vbo.Bind();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _skyTexture);
        _shader.SetUniform("uModel", _transform.ModelMatrix);
        _shader.SetUniform("uSkyTexture", 0);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
        _gl.Enable(EnableCap.DepthTest);
    }

    public override void OnClose()
    {
        if (_skyTexture != 0)
        {
            _gl.DeleteTexture(_skyTexture);
            _skyTexture = 0;
        }

        base.OnClose();
    }

    private static byte[] BuildSkyTexture(int width, int height)
    {
        byte[] data = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            float t = y / (height - 1f);
            for (int x = 0; x < width; x++)
            {
                float wave = MathF.Sin(x * 0.17f + y * 0.07f) * 0.5f + 0.5f;
                float cloud = MathF.Max(0f, wave - 0.63f) * (1f - MathF.Abs(t - 0.58f) * 2.2f);
                byte r = (byte)Math.Clamp(80 + t * 80 + cloud * 80, 0, 255);
                byte g = (byte)Math.Clamp(132 + t * 75 + cloud * 60, 0, 255);
                byte b = (byte)Math.Clamp(205 + t * 35 + cloud * 35, 0, 255);
                int i = (y * width + x) * 4;
                data[i] = r;
                data[i + 1] = g;
                data[i + 2] = b;
                data[i + 3] = 255;
            }
        }

        return data;
    }
}
