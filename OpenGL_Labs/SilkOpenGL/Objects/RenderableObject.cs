using Silk.NET.OpenGL;
using SilkOpenGL.Store;

namespace SilkOpenGL.Objects;

public abstract class RenderableObject
{
    protected GL _gl;

    protected BufferObject<float> _vbo;
    protected BufferObject<uint> _ebo;
    protected VertexArrayObject<float, uint> _vao;

    protected float[] _vertices;
    protected uint[] _indices;

    public string ShaderKey { get; }
    protected Shader _shader;

    public string? TextureKey { get; }
    protected Texture? _texture;
    
    protected Transform _transform;

    private bool _initialized;

    public RenderableObject(string shaderKey)
    {
        ShaderKey = shaderKey;
    }

    public RenderableObject(string shaderKey, string textureKey)
    {
        ShaderKey = shaderKey;
        TextureKey = textureKey;
    }

    public virtual void Init(ShaderStore shaderStore, TextureStore textureStore, GL gl)
    {
        if (_initialized) return;
        _gl = gl;
        _shader = shaderStore.GetShader(ShaderKey);
        _texture = TextureKey != null ? textureStore.GetTexture(TextureKey) : null;

        OnInit();
        _initialized = true;
    }

    protected abstract void OnInit();
    public abstract void OnUpdate(double dt);
    public abstract void OnRender(double dt);

    public virtual void BindResources()
    {
        /* Bind texture, set uniforms */
    }

    public virtual void OnClose()
    {
        /* Dispose VBO, etc. */
    }
}