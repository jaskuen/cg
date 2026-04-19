using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL.Store;

namespace SilkOpenGL.Objects;

public abstract class RenderableObject : UpdateableObject, IDisposable
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

    public string? MaterialKey { get; }
    protected Material? _material;

    protected internal Transform _transform;

    private bool _initialized;

    public Vector3 Position => _transform.Position;
    public bool ClearDepthBeforeRender { get; set; }

    private RenderableObject()
    {
        _transform = new Transform();
    }

    public RenderableObject(string shaderKey) : this()
    {
        ShaderKey = shaderKey;
    }

    public RenderableObject(string shaderKey, string? textureKey, bool isMaterial = false) : this(shaderKey)
    {
        if (string.IsNullOrWhiteSpace(textureKey))
        {
            return;
        }

        if (isMaterial)
        {
            MaterialKey = textureKey;
        }
        else
        {
            TextureKey = textureKey;
        }
    }

    public void Render(double dt)
    {
        if (_vao != null)
        {
            _vao.Bind();
        }

        OnRender(dt);

        _gl.BindVertexArray(0);
    }

    public abstract void OnRender(double dt);

    public virtual void Init(ShaderStore shaderStore, TextureStore textureStore, MaterialStore materialStore, GL gl)
    {
        if (_initialized) return;
        _gl = gl;
        _shader = shaderStore.GetShader(ShaderKey);
        if (TextureKey != null && !textureStore.ContainsTexture(TextureKey) && File.Exists(TextureKey))
        {
            textureStore.CreateTexture(TextureKey, TextureKey);
        }

        _texture = TextureKey != null && textureStore.ContainsTexture(TextureKey)
            ? textureStore.GetTexture(TextureKey)
            : null;
        _material = MaterialKey != null ? materialStore.GetMaterial(MaterialKey) : null;

        OnInit();
        _initialized = true;
    }

    protected abstract void OnInit();

    public virtual void BindResources()
    {
        if (_texture != null)
        {
            _texture.Bind();
            _shader.TrySetUniform("uTexture", 0);
            _shader.TrySetUniform("uTextureId", _texture.TextureId);
            _shader.TrySetUniform("uHandle", _texture.TextureId);
            _shader.TrySetUniform("uHasTexture", 1);
        }
        else
        {
            _shader.TrySetUniform("uTexture", 0);
            _shader.TrySetUniform("uTextureId", 0);
            _shader.TrySetUniform("uHandle", 0);
            _shader.TrySetUniform("uHasTexture", 0);
        }

        _shader.TrySetUniform("uMaterial.baseColor", Vector3.One);

        if (_material != null)
        {
            if (_material.Albedo != null)
            {
                _shader.TrySetUniform("uMaterial.albedoMap", _material.Albedo.TextureId);
                _shader.TrySetUniform("uMaterial.hasAlbedoMap", 1);
            }
            else
            {
                _shader.TrySetUniform("uMaterial.hasAlbedoMap", 0);
            }

            if (_material.Normal != null)
            {
                _shader.TrySetUniform("uMaterial.normalMap", _material.Normal.TextureId);
                _shader.TrySetUniform("uMaterial.hasNormalMap", 1);
            }
            else
            {
                _shader.TrySetUniform("uMaterial.hasNormalMap", 0);
            }

            if (_material.Metallic != null)
            {
                _shader.TrySetUniform("uMaterial.metallicMap", _material.Metallic.TextureId);
                _shader.TrySetUniform("uMaterial.hasMetallicMap", 1);
            }
            else
            {
                _shader.TrySetUniform("uMaterial.hasMetallicMap", 0);
            }

            if (_material.Roughness != null)
            {
                _shader.TrySetUniform("uMaterial.roughnessMap", _material.Roughness.TextureId);
                _shader.TrySetUniform("uMaterial.hasRoughnessMap", 1);
            }
            else
            {
                _shader.TrySetUniform("uMaterial.hasRoughnessMap", 0);
            }

            if (_material.Ao != null)
            {
                _shader.TrySetUniform("uMaterial.aoMap", _material.Ao.TextureId);
                _shader.TrySetUniform("uMaterial.hasAoMap", 1);
            }
            else
            {
                _shader.TrySetUniform("uMaterial.hasAoMap", 0);
            }
        }
    }

    public Vector3[] GetWorldVertices(int verticesCount)
    {
        int vertexCount = _vertices.Length / verticesCount;
        Vector3[] worldPoints = new Vector3[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 localPos = new Vector3(
                _vertices[i * 3],
                _vertices[i * 3 + 1],
                _vertices[i * 3 + 2]
            );

            worldPoints[i] = Vector3.Transform(localPos, _transform.ModelMatrix);
        }

        return worldPoints;
    }

    public virtual void OnClose()
    {
        _gl?.BindVertexArray(0);
        _gl?.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl?.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();

        _vbo = null;
        _ebo = null;
        _vao = null;
    }

    public void Dispose()
    {
        OnClose();
    }

    public virtual unsafe void OnRenderPicking(GL gl, Shader pickingShader)
    {
        _vao.Bind();
        pickingShader.Use();
        pickingShader.SetUniform("uModel", _transform.ModelMatrix);

        gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }
}
