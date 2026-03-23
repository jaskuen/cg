namespace SilkOpenGL.Store;

public class StoreManager
{
    private readonly ShaderStore _shaderStore;
    private readonly TextureStore _textureStore;
    private readonly FontStore _fontStore;
    private readonly MaterialStore _materialStore;
    
    internal ShaderStore ShaderStore => _shaderStore;
    internal TextureStore TextureStore => _textureStore;
    internal FontStore FontStore => _fontStore;
    internal MaterialStore MaterialStore => _materialStore;

    public StoreManager()
    {
        _fontStore = new FontStore();
        _shaderStore = new ShaderStore();
        _textureStore = new TextureStore();
        _materialStore = new MaterialStore(_textureStore);
    }

    public void AddShader(string key, string vertexPath, string fragmentPath)
    {
        _shaderStore.CreateShader(key, vertexPath, fragmentPath);
    }

    public void AddTexture(string key, string path)
    {
        _textureStore.CreateTexture(key, path);
    }

    public void AddFont(string key, string path)
    {
        _fontStore.CreateFont(key, path);
    }

    public void AddMaterial(string key, string path)
    {
        _materialStore.CreateMaterial(key, path);
    }
}