using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL.Objects;

namespace SilkOpenGL;

public class PickingService
{
    private GL _gl;
    private uint _fbo;
    private uint _colorTexture;
    private uint _depthRenderBuffer;
    private int _width, _height;

    private readonly PickingRegistry _registry;

    public PickingService(int width, int height)
    {
        _registry = new PickingRegistry();

        _width = width;
        _height = height;
    }

    public unsafe void SetupFramebuffer(GL gl)
    {
        _gl = gl;

        UpdateFramebuffer();
    }

    private unsafe void UpdateFramebuffer()
    {
        _fbo = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        _colorTexture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _colorTexture);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)_width, (uint)_height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, null);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _colorTexture, 0);

        _depthRenderBuffer = _gl.GenRenderbuffer();
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRenderBuffer);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)_width,
            (uint)_height);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, _depthRenderBuffer);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void UpdateViewport(int width, int height)
    {
        _width = width;
        _height = height;
        UpdateFramebuffer();
    }

    public uint ReadIdAt(int x, int y)
    {
        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);

        int flippedY = _height - y;

        byte[] pixel = new byte[4];
        unsafe
        {
            fixed (byte* ptr = pixel)
            {
                _gl.ReadPixels(x, flippedY, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

        return (uint)(pixel[0] | (pixel[1] << 8) | (pixel[2] << 16));
    }

    public void BindForRendering() => _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

    public uint Register(IClickable obj) => _registry.Register(obj);
    public void Unregister(IClickable obj) => _registry.Unregister(obj);

    public IClickable? GetObjectById(uint id) => _registry.GetObject(id);

    public PickingRegistry GetRegistry() => _registry;

    public Vector3 IdToColor(uint id)
    {
        return new Vector3(
            (id & 0xFF) / 255.0f,
            ((id >> 8) & 0xFF) / 255.0f,
            ((id >> 16) & 0xFF) / 255.0f
        );
    }
}