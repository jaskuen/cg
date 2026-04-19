using Silk.NET.OpenGL;
using SilkOpenGL.Objects;

namespace SilkOpenGL.Model;

public class ModelObject : RenderableObject
{
    private readonly ModelData _modelData;
    private readonly List<RenderableMesh> _meshes = [];
    private readonly string? _defaultTextureKey;
    private readonly string? _defaultMaterialKey;
    private readonly bool _useDefaultMaterial;
    private bool _isInitialized;

    private DateTime _changeMesh;
    private int _currMeshIndex = 0;

    internal List<RenderableObject> Meshes => _meshes.Select(RenderableObject (x) => x).ToList();

    public ModelObject(string shaderKey, ModelData modelData) : base(shaderKey)
    {
        _modelData = modelData ?? throw new ArgumentNullException(nameof(modelData));

        foreach (ModelMeshData meshData in _modelData.Meshes)
        {
            string? textureKey = _useDefaultMaterial ? null : meshData.TextureKey ?? _defaultTextureKey;
            string? materialKey =
                _useDefaultMaterial ? meshData.MaterialKey ?? _defaultMaterialKey : meshData.MaterialKey;

            _meshes.Add(new RenderableMesh(this, meshData, ShaderKey, textureKey ?? materialKey, false));
        }
    }

    public ModelObject(string shaderKey, ModelData modelData, string resourceKey, bool isMaterial)
        : this(shaderKey, modelData)
    {
        if (isMaterial)
        {
            _defaultMaterialKey = resourceKey;
            _useDefaultMaterial = true;
        }
        else
        {
            _defaultTextureKey = resourceKey;
        }

        foreach (ModelMeshData meshData in _modelData.Meshes)
        {
            string? textureKey = _useDefaultMaterial ? null : meshData.TextureKey ?? _defaultTextureKey;
            string? materialKey =
                _useDefaultMaterial ? meshData.MaterialKey ?? _defaultMaterialKey : meshData.MaterialKey;

            _meshes.Add(new RenderableMesh(this, meshData, ShaderKey, isMaterial ? materialKey : textureKey, isMaterial));
        }
    }

    public Transform Transform => _transform;

    public override void BindResources()
    {
        ResetMaterialUniforms();
    }

    protected override void OnInit()
    {
    }

    public override void OnUpdate(double dt)
    {
    }

    public override unsafe void OnRender(double dt)
    {
    }

    private unsafe void RenderCurrentMesh()
    {
        if (DateTime.Now > _changeMesh)
        {
            _changeMesh = DateTime.Now.AddSeconds(0.5);
            _currMeshIndex++;
            if (_currMeshIndex >= _meshes.Count)
            {
                _currMeshIndex = 0;
            }

            Console.WriteLine(_currMeshIndex);
        }

        RenderableMesh mesh = _meshes[_currMeshIndex];

        _gl.DrawElements(mesh.DrawPrimitive, (uint)mesh.IndexCount, DrawElementsType.UnsignedInt, null);
    }

    public override void OnClose()
    {
        foreach (RenderableMesh mesh in _meshes)
        {
            mesh.Dispose();
        }

        _meshes.Clear();
        _vertices = [];
        _indices = [];

        base.OnClose();
    }

    private void BindMeshResources(RenderableMesh mesh)
    {
        if (mesh.Texture != null)
        {
            mesh.Texture.Bind();
            _shader.TrySetUniform("uTextureId", mesh.Texture.TextureId);
            _shader.TrySetUniform("uHandle", mesh.Texture.TextureId);
            _shader.TrySetUniform("uHasTexture", 1);
        }
        else
        {
            _shader.TrySetUniform("uTextureId", 0);
            _shader.TrySetUniform("uHandle", 0);
            _shader.TrySetUniform("uHasTexture", 0);
        }

        Material? material = mesh.Material;
        if (material == null)
        {
            ResetMaterialUniforms();
            return;
        }

        if (material.Albedo != null)
        {
            _shader.TrySetUniform("uMaterial.albedoMap", material.Albedo.TextureId);
            _shader.TrySetUniform("uMaterial.hasAlbedoMap", 1);
        }
        else
        {
            _shader.TrySetUniform("uMaterial.hasAlbedoMap", 0);
        }

        if (material.Normal != null)
        {
            _shader.TrySetUniform("uMaterial.normalMap", material.Normal.TextureId);
            _shader.TrySetUniform("uMaterial.hasNormalMap", 1);
        }
        else
        {
            _shader.TrySetUniform("uMaterial.hasNormalMap", 0);
        }

        if (material.Metallic != null)
        {
            _shader.TrySetUniform("uMaterial.metallicMap", material.Metallic.TextureId);
            _shader.TrySetUniform("uMaterial.hasMetallicMap", 1);
        }
        else
        {
            _shader.TrySetUniform("uMaterial.hasMetallicMap", 0);
        }

        if (material.Roughness != null)
        {
            _shader.TrySetUniform("uMaterial.roughnessMap", material.Roughness.TextureId);
            _shader.TrySetUniform("uMaterial.hasRoughnessMap", 1);
        }
        else
        {
            _shader.TrySetUniform("uMaterial.hasRoughnessMap", 0);
        }

        if (material.Ao != null)
        {
            _shader.TrySetUniform("uMaterial.aoMap", material.Ao.TextureId);
            _shader.TrySetUniform("uMaterial.hasAoMap", 1);
        }
        else
        {
            _shader.TrySetUniform("uMaterial.hasAoMap", 0);
        }
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
    }

    private static ( float[] Vertices, uint[] Indices ) BuildCombinedGeometry(IReadOnlyList<ModelMeshData> meshes)
    {
        List<float> vertices = [];
        List<uint> indices = [];
        uint vertexOffset = 0;

        foreach (ModelMeshData mesh in meshes)
        {
            vertices.AddRange(mesh.Vertices);

            foreach (uint index in mesh.Indices)
            {
                indices.Add(index + vertexOffset);
            }

            vertexOffset += (uint)(mesh.Vertices.Length / mesh.VertexStride);
        }

        return (vertices.ToArray(), indices.ToArray());
    }
}