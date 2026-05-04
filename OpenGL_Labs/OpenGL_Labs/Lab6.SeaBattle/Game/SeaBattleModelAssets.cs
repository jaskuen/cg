using System.Numerics;
using SilkOpenGL.Model;

namespace Lab6.SeaBattle.Game;

internal sealed class SeaBattleModelAssets
{
    private readonly Dictionary<ShipVariant, ModelAsset> _shipAssets;

    public SeaBattleModelAssets()
    {
        _shipAssets = new Dictionary<ShipVariant, ModelAsset>
        {
            [ShipVariant.Destroyer] = LoadShip("Models/warship1.glb", 3.2f),
            [ShipVariant.Cruiser] = LoadShip("Models/warship2.glb", 3.8f),
            [ShipVariant.Carrier] = LoadShip("Models/warship3.glb", 4.5f)
        };
        Torpedo = LoadForwardModel("Models/torpedo.glb", 0.95f, includeYForForward: true);
    }

    public ModelAsset Torpedo { get; }

    public ModelAsset Ship(ShipVariant variant) => _shipAssets[variant];

    private static ModelAsset LoadShip(string path, float targetLength)
    {
        return LoadForwardModel(path, targetLength, includeYForForward: false);
    }

    private static ModelAsset LoadForwardModel(string path, float targetLength, bool includeYForForward)
    {
        ModelData data = ModelLoader.Load(path);
        ModelBounds bounds = CalculateBounds(data);
        Vector3 size = bounds.Size;
        float length = includeYForForward
            ? MathF.Max(size.X, MathF.Max(size.Y, size.Z))
            : MathF.Max(size.X, size.Z);
        float scale = length > 0.0001f ? targetLength / length : 1f;
        Vector3 forwardAxis = DominantAxis(size, includeYForForward);
        float waterlineOffset = -bounds.Min.Y * scale;

        return new ModelAsset(data, scale, forwardAxis, waterlineOffset);
    }

    private static Vector3 DominantAxis(Vector3 size, bool includeY)
    {
        if (includeY && size.Y >= size.X && size.Y >= size.Z)
        {
            return Vector3.UnitY;
        }

        return size.X >= size.Z ? Vector3.UnitX : Vector3.UnitZ;
    }

    private static ModelBounds CalculateBounds(ModelData data)
    {
        Vector3 min = new(float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity);

        foreach (ModelMeshData mesh in data.Meshes)
        {
            for (int i = 0; i < mesh.Vertices.Length; i += mesh.VertexStride)
            {
                Vector3 local = new(mesh.Vertices[i], mesh.Vertices[i + 1], mesh.Vertices[i + 2]);
                Vector3 transformed = Vector3.Transform(local, mesh.LocalTransform);
                min = Vector3.Min(min, transformed);
                max = Vector3.Max(max, transformed);
            }
        }

        return new ModelBounds(min, max);
    }
}

internal sealed record ModelAsset(
    ModelData Data,
    float Scale,
    Vector3 ForwardAxis,
    float WaterlineOffset);

internal readonly record struct ModelBounds(Vector3 Min, Vector3 Max)
{
    public Vector3 Size => Max - Min;
}
