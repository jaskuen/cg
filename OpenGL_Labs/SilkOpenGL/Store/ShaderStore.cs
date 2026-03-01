namespace SilkOpenGL.Store;

public class ShaderStore : IDisposable
{
    private readonly Dictionary<string, Shader> _shaders = [];

    public void CreateShader(string shaderKey, string vertexSource, string fragmentSource)
    {
        _shaders.Add(shaderKey, new Shader(vertexSource, fragmentSource));
    }

    public Shader GetShader(string shaderKey)
    {
        return _shaders[shaderKey];
    }

    public Dictionary<string, Shader> AllShaders => _shaders;

    public void Dispose()
    {
        foreach (var shader in _shaders.Values)
        {
            shader.Dispose();
        }

        _shaders.Clear();
    }
}