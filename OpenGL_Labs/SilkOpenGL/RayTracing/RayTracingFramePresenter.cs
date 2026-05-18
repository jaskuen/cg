using Silk.NET.OpenGL;

namespace SilkOpenGL.RayTracing;

public class RayTracingFramePresenter : IDisposable
{
    private readonly GL _gl;
    private readonly Shader _shader;
    private readonly uint _texture;
    private readonly BufferObject<float> _vbo;
    private readonly BufferObject<uint> _ebo;
    private readonly VertexArrayObject<float, uint> _vao;
    private int _width;
    private int _height;

    private static readonly float[] Vertices =
    [
        -1f, -1f, 0f, 1f,
        1f, -1f, 1f, 1f,
        1f, 1f, 1f, 0f,
        -1f, 1f, 0f, 0f
    ];

    private static readonly uint[] Indices = [0, 1, 2, 2, 3, 0];

    public RayTracingFramePresenter(GL gl, Shader shader)
    {
        _gl = gl;
        _shader = shader;
        _texture = _gl.GenTexture();

        _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 2, VertexAttribPointerType.Float, 4, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 4, 2);

        BindTexture();
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
    }

    public unsafe void Present(byte[] framebuffer, int width, int height)
    {
        BindTexture();

        fixed (byte* ptr = framebuffer)
        {
            if (_width != width || _height != height)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                _width = width;
                _height = height;
            }
            else
            {
                _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)width, (uint)height,
                    PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        _gl.Disable(EnableCap.DepthTest);
        _shader.Use();
        _shader.TrySetUniform("uFrame", 0);
        _vao.Bind();
        _gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
        _gl.BindVertexArray(0);
        _gl.Enable(EnableCap.DepthTest);
    }

    private void BindTexture()
    {
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);
    }

    public void Dispose()
    {
        _vao.Dispose();
        _ebo.Dispose();
        _vbo.Dispose();
        _gl.DeleteTexture(_texture);
    }
}
