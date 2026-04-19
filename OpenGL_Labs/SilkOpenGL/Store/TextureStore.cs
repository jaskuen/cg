using Silk.NET.OpenGL;

namespace SilkOpenGL.Store;

public class TextureStore : IDisposable
{
    private readonly Dictionary<string, Texture> _textures = [];
    private bool _handlesDirty = true;

    public void CreateTexture(string textureKey, string path)
    {
        if (_textures.ContainsKey(textureKey))
        {
            return;
        }

        _textures.Add(textureKey, new Texture(path, _textures.Count));
        _handlesDirty = true;
    }

    public Texture GetTexture(string textureKey)
    {
        return _textures[textureKey];
    }

    public bool CompilePending(GL gl)
    {
        bool compiledAny = false;

        foreach (Texture texture in _textures.Values)
        {
            if (texture.IsCompiled)
            {
                continue;
            }

            texture.Compile(gl);
            compiledAny = true;
        }

        return compiledAny;
    }

    public bool NeedsHandleUpload()
    {
        return _handlesDirty || _textures.Values.Any(texture => !texture.IsCompiled);
    }

    public void MarkHandlesUploaded()
    {
        _handlesDirty = false;
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
