using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Lighting;
using SilkOpenGL.Store;

namespace Lab4.Labyrinth;

class Program
{
    static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Title = "Labyrinth";
        options.Size = new Vector2D<int>(1280, 720);

        StoreManager storeManager = new StoreManager();

        storeManager.AddShader("PbrShader", "Shaders/pbr.vert", "Shaders/pbr.frag");
        storeManager.AddShader("FloorShader", "Shaders/shader.vert", "Shaders/shader.frag");

        storeManager.AddMaterial("MetalMaterial", "Textures/Metal/metal.json");
        storeManager.AddMaterial("GoldMaterial", "Textures/Gold/gold.json");
        storeManager.AddMaterial("LeatherMaterial", "Textures/Leather/leather.json");
        storeManager.AddMaterial("SnowMaterial", "Textures/Snow/snow.json");
        storeManager.AddMaterial("MossyMaterial", "Textures/MossyTiles/mossy.json");
        storeManager.AddMaterial("TilesMaterial", "Textures/Tiles/tiles.json");

        PlayerObject player = new PlayerObject();
        player.Position = new Vector3(-6.5f, 0, -6.5f);

        World world = new World(options, storeManager, player, true);

        var materialMap = new Dictionary<WallType, string>
        {
            { WallType.Metal, "MetalMaterial" },
            { WallType.Gold, "GoldMaterial" },
            { WallType.Snow, "SnowMaterial" },
            { WallType.Mossy, "MossyMaterial" },
            { WallType.Tiles, "TilesMaterial" },
            { WallType.Leather, "LeatherMaterial" },
        };

        int[,] map = new int[,]
        {
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            { 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1 },
            { 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1 },
            { 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 1 },
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1 },
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1 },
            { 1, 0, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1 },
            { 1, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 },
            { 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 },
            { 1, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 0, 1, 0, 1 },
            { 1, 0, 1, 0, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1 },
            { 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            { 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        };
        
        Random random = new Random();

        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                if (map[i, j] == 1)
                {
                    map[i, j] = random.Next(1, 6);
                }
            }
        }

        var labyrinth = new LabyrinthField(world, player, map, materialMap, "PbrShader");
        world.AddObject(labyrinth);

        world.AddLight(new LightEntity(new Vector3(0, 3.5f, 5f), new Vector3(1.0f, 0.9f, 0.8f))
        {
            Ambient = new Vector3(0.2f),
            Specular = new Vector3(1.0f, 1.0f, 1.0f),
            Intensity = 3.0f,
            Linear = 0.09f,
            Quadratic = 0.032f
        });

        world.Run();
    }
}
