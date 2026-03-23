using System.Text.Json.Serialization;

namespace SilkOpenGL;

public class MaterialData
{
    [JsonPropertyName("albedo")]
    public string? Albedo { get; set; }

    [JsonPropertyName("normal")]
    public string? Normal { get; set; }

    [JsonPropertyName("metallic")]
    public string? Metallic { get; set; }

    [JsonPropertyName("roughness")]
    public string? Roughness { get; set; }

    [JsonPropertyName("ao")]
    public string? Ao { get; set; }
}

public class Material
{
    public Texture? Albedo { get; set; }
    public Texture? Normal { get; set; }
    public Texture? Metallic { get; set; }
    public Texture? Roughness { get; set; }
    public Texture? Ao { get; set; }
}
