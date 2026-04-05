using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ARB;
using StbImageSharp;

namespace SilkOpenGL;

public class Texture : IDisposable
{
    private uint _handle;
    private GL _gl;
    private string _path;
    private bool _isCompiled;

    public ulong BindlessHandle { get; private set; }

    public int TextureId { get; private set; }
    public bool IsCompiled => _isCompiled;

    private ArbBindlessTexture _bindlessExt;
    private bool _isResident;

    public Texture( string path, int index )
    {
        _path = path;
        TextureId = index;
    }

    public unsafe void Compile( GL gl )
    {
        if ( _isCompiled ) return;
        if ( gl == null ) throw new ArgumentNullException( nameof( gl ) );
        _gl = gl;

        _handle = _gl.GenTexture();
        Bind();

        ImageResult result = ImageResult.FromMemory( File.ReadAllBytes( _path ), ColorComponents.RedGreenBlueAlpha );
        fixed ( byte* ptr = result.Data )
        {
            _gl.TexImage2D( TextureTarget.Texture2D, 0, InternalFormat.Rgba, ( uint )result.Width,
                ( uint )result.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
        }

        SetParameters();

        SetupBindless();

        _isCompiled = true;
    }

    private void SetupBindless()
    {
        if ( _gl.TryGetExtension( out _bindlessExt ) )
        {
            BindlessHandle = _bindlessExt.GetTextureHandle( _handle );
            _bindlessExt.MakeTextureHandleResident( BindlessHandle );
            _isResident = true;
        }
        else
        {
            Console.WriteLine( "Видеокарта не поддерживает GL_ARB_bindless_texture!" );
        }
    }

    private void SetParameters()
    {
        _gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ( int )GLEnum.ClampToEdge );
        _gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ( int )GLEnum.ClampToEdge );
        _gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            ( int )GLEnum.LinearMipmapLinear );
        _gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ( int )GLEnum.Linear );
        _gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0 );
        _gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8 );
        _gl.GenerateMipmap( TextureTarget.Texture2D );
    }

    public void Bind( TextureUnit textureSlot = TextureUnit.Texture0 )
    {
        _gl.ActiveTexture( textureSlot );
        _gl.BindTexture( TextureTarget.Texture2D, _handle );
    }

    public void Dispose()
    {
        if ( _isResident && _bindlessExt != null )
        {
            _bindlessExt.MakeTextureHandleNonResident( BindlessHandle );
            _bindlessExt.Dispose();
        }

        _gl.DeleteTexture( _handle );
    }
}
