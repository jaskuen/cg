namespace SilkOpenGL.Store;

public class TextureStore : IDisposable
{
    private readonly Dictionary<string, Texture> _textures = [];

    public void CreateTexture(string textureKey, string path)
    {
        _textures.Add(textureKey, new Texture(path, _textures.Count));
    }

    public Texture GetTexture(string textureKey)
    {
        return _textures[textureKey];
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
    }
    
    public Dictionary<string, Texture> AllTextures => _textures;
}