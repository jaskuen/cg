using System.Text.Json;

namespace SilkOpenGL.Store;

public class MaterialStore
{
    private readonly Dictionary<string, Material> _materials = [];
    private readonly TextureStore _textureStore;

    public MaterialStore(TextureStore textureStore)
    {
        _textureStore = textureStore;
    }

    public void CreateMaterial(string materialKey, string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Material file not found: {jsonPath}");

        var json = File.ReadAllText(jsonPath);
        var data = JsonSerializer.Deserialize<MaterialData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data == null)
            throw new Exception($"Failed to parse material JSON: {jsonPath}");

        var dir = Path.GetDirectoryName(jsonPath) ?? string.Empty;

        var material = new Material();
        
        material.Albedo = GetOrLoadTexture(materialKey, "albedo", data.Albedo, dir);
        material.Normal = GetOrLoadTexture(materialKey, "normal", data.Normal, dir);
        material.Metallic = GetOrLoadTexture(materialKey, "metallic", data.Metallic, dir);
        material.Roughness = GetOrLoadTexture(materialKey, "roughness", data.Roughness, dir);
        material.Ao = GetOrLoadTexture(materialKey, "ao", data.Ao, dir);

        _materials[materialKey] = material;
    }

    private Texture? GetOrLoadTexture(string materialKey, string type, string? relativePath, string baseDir)
    {
        if (string.IsNullOrEmpty(relativePath)) return null;

        var fullPath = Path.Combine(baseDir, relativePath);
        fullPath = Path.GetFullPath(fullPath);

        var textureKey = fullPath; 

        if (!_textureStore.AllTextures.ContainsKey(textureKey))
        {
            _textureStore.CreateTexture(textureKey, fullPath);
        }

        return _textureStore.GetTexture(textureKey);
    }

    public Material GetMaterial(string materialKey)
    {
        return _materials[materialKey];
    }

    public Dictionary<string, Material> AllMaterials => _materials;
}