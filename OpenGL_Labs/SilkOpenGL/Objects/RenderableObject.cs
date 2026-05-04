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

    public Transform Transform => _transform;

    private bool _initialized;

    public Vector3 Position => _transform.Position;
    public bool ClearDepthBeforeRender { get; set; }

    protected virtual Matrix4x4 WorldModelMatrix => _transform.ModelMatrix;

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

        ResetMaterialUniforms();

        if (_material == null)
        {
            if (_texture != null)
            {
                _shader.TrySetUniform("uMaterial.albedoMap", _texture.TextureId);
                _shader.TrySetUniform("uMaterial.hasAlbedoMap", 1);
            }

            return;
        }

        BindMaterialUniforms(_material);
    }

    private void ResetMaterialUniforms()
    {
        _shader.TrySetUniform("uMaterial.albedoMap", 0);
        _shader.TrySetUniform("uMaterial.normalMap", 0);
        _shader.TrySetUniform("uMaterial.metallicMap", 0);
        _shader.TrySetUniform("uMaterial.roughnessMap", 0);
        _shader.TrySetUniform("uMaterial.aoMap", 0);
        _shader.TrySetUniform("uMaterial.hasAlbedoMap", 0);
        _shader.TrySetUniform("uMaterial.hasNormalMap", 0);
        _shader.TrySetUniform("uMaterial.hasMetallicMap", 0);
        _shader.TrySetUniform("uMaterial.hasRoughnessMap", 0);
        _shader.TrySetUniform("uMaterial.hasAoMap", 0);
        _shader.TrySetUniform("uMaterial.baseColor", Vector3.One);
    }

    private void BindMaterialUniforms(Material material)
    {
        if (material.Albedo != null)
        {
            _shader.TrySetUniform("uMaterial.albedoMap", material.Albedo.TextureId);
            _shader.TrySetUniform("uMaterial.hasAlbedoMap", 1);
        }

        if (material.Normal != null)
        {
            _shader.TrySetUniform("uMaterial.normalMap", material.Normal.TextureId);
            _shader.TrySetUniform("uMaterial.hasNormalMap", 1);
        }

        if (material.Metallic != null)
        {
            _shader.TrySetUniform("uMaterial.metallicMap", material.Metallic.TextureId);
            _shader.TrySetUniform("uMaterial.hasMetallicMap", 1);
        }

        if (material.Roughness != null)
        {
            _shader.TrySetUniform("uMaterial.roughnessMap", material.Roughness.TextureId);
            _shader.TrySetUniform("uMaterial.hasRoughnessMap", 1);
        }

        if (material.Ao != null)
        {
            _shader.TrySetUniform("uMaterial.aoMap", material.Ao.TextureId);
            _shader.TrySetUniform("uMaterial.hasAoMap", 1);
        }
    }

    public Vector3[] GetWorldVertices(int verticesCount)
    {
        if (verticesCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(verticesCount), "Vertex stride must be positive.");
        }

        int vertexCount = _vertices.Length / verticesCount;
        Vector3[] worldPoints = new Vector3[vertexCount];
        Matrix4x4 modelMatrix = WorldModelMatrix;

        for (int i = 0; i < vertexCount; i++)
        {
            int vertexOffset = i * verticesCount;
            Vector3 localPos = new Vector3(
                _vertices[vertexOffset],
                _vertices[vertexOffset + 1],
                _vertices[vertexOffset + 2]
            );

            worldPoints[i] = Vector3.Transform(localPos, modelMatrix);
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
        _initialized = false;
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