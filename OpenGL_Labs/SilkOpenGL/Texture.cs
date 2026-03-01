using Silk.NET.OpenGL;
using StbImageSharp;

namespace SilkOpenGL;

public class Texture : IDisposable
{
    private uint _handle;
    private GL _gl;

    private string _path;
    private bool _isCompiled;

    public Texture(string path)
    {
        _path = path;
    }

    public unsafe void Compile(GL gl)
    {
        if (_isCompiled) return;
        if (gl == null) throw new ArgumentNullException(nameof(gl));

        _gl = gl;

        //Generating the opengl handle;
        _handle = _gl.GenTexture();
        Bind();

        // Load the image from memory.
        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(_path), ColorComponents.RedGreenBlueAlpha);

        fixed (byte* ptr = result.Data)
        {
            // Create our texture and upload the image data.
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)result.Width,
                (uint)result.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }

        SetParameters();

        _isCompiled = true;
    }

    private void SetParameters()
    {
        //Setting some texture perameters so the texture behaves as expected.
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)GLEnum.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);

        //Generating mipmaps.
        _gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
    {
        //When we bind a texture we can choose which textureslot we can bind it to.
        _gl.ActiveTexture(textureSlot);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose()
    {
        //In order to dispose we need to delete the opengl handle for the texure.
        _gl.DeleteTexture(_handle);
    }
}