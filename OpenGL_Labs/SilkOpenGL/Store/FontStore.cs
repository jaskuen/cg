using SilkOpenGL.Text;

namespace SilkOpenGL.Store;

public class FontStore
{
    private readonly Dictionary<string, FontMetadata> _fonts = [];

    public void CreateFont(string fontKey, string path)
    {
        _fonts.Add(fontKey, new FontMetadata(path));
    }

    public FontMetadata GetMetadata(string fontKey)
    {
        return _fonts[fontKey];
    }
    
    public Dictionary<string, FontMetadata> AllFonts => _fonts;
}