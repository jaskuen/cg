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

        PlayerObject player = new PlayerObject();
        player.Position = new Vector3(-6.5f, 0, -6.5f);

        World world = new World(options, storeManager, player);

        var materialMap = new Dictionary<WallType, string>
        {
            { WallType.Metal, "MetalMaterial" }
        };

        int[,] map = new int[,]
        {
            { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 },
            { 3, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3 },
            { 3, 0, 3, 0, 3, 0, 3, 3, 3, 3, 3, 3, 3, 3, 0, 3 },
            { 3, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 3 },
            { 3, 0, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0, 3, 0, 3 },
            { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 3, 0, 3 },
            { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 0, 3, 0, 3, 0, 3 },
            { 3, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 3, 0, 3, 0, 3 },
            { 3, 0, 3, 3, 3, 3, 3, 3, 0, 3, 0, 3, 0, 3, 0, 3 },
            { 3, 0, 3, 0, 0, 0, 0, 3, 0, 3, 0, 3, 0, 3, 0, 3 },
            { 3, 0, 3, 0, 3, 3, 0, 3, 0, 3, 0, 3, 0, 3, 0, 3 },
            { 3, 0, 3, 0, 3, 0, 0, 3, 0, 3, 0, 0, 0, 3, 0, 3 },
            { 3, 0, 3, 0, 3, 3, 3, 3, 0, 3, 3, 3, 3, 3, 0, 3 },
            { 3, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3 },
            { 3, 0, 0, 0, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 },
            { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }
        };

        var labyrinth = new LabyrinthField(world, player, map, materialMap, "PbrShader");
        world.AddObject(labyrinth);

        world.AddLight(new LightEntity(new Vector3(0, 1.5f, 5f), new Vector3(1.0f, 0.9f, 0.8f))
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
