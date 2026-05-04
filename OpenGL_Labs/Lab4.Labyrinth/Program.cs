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
        options.Title = "Lab 4 - PBR Labyrinth";
        options.Size = new Vector2D<int>(1280, 720);

        StoreManager storeManager = new StoreManager();

        // 1. Load PBR Shaders
        storeManager.AddShader("PbrShader", "Shaders/pbr.vert", "Shaders/pbr.frag");

        // 2. Load Materials
        // Note: Make sure "Textures/Metal/metal.json" has paths correctly pointing to the metal textures
        storeManager.AddMaterial("MetalMaterial", "Textures/Metal/metal.json");

        // Use the FPS camera mode to walk around the labyrinth
        World world = new World(options, storeManager);

        // 3. Define mapping from WallType to Material Keys
        var materialMap = new Dictionary<WallType, string>
        {
            { WallType.Metal, "MetalMaterial" }
            // Add other materials here like WallType.Brick -> "BrickMaterial"
        };

        // 4. Create the Labyrinth map (3 = Metal, 0 = Empty)
        int[,] map = new int[,]
        {
            { 3, 3, 3, 3, 3, 3, 3 },
            { 3, 0, 0, 0, 0, 0, 3 },
            { 3, 0, 3, 3, 3, 0, 3 },
            { 3, 0, 0, 0, 3, 0, 3 },
            { 3, 3, 3, 0, 3, 0, 3 },
            { 3, 0, 0, 0, 0, 0, 3 },
            { 3, 3, 3, 3, 3, 3, 3 }
        };

        // 5. Build the labyrinth and add it to the world
        var labyrinth = new LabyrinthField(world, map, materialMap, "PbrShader");
        world.AddObject(labyrinth);

        // 6. Add some lighting
        // Center light
        world.AddLight(new LightEntity(new Vector3(0, 1.5f, 5f), new Vector3(1.0f, 0.9f, 0.8f))
        {
            Ambient = new Vector3(0.2f),
            Specular = new Vector3(1.0f, 1.0f, 1.0f),
            Intensity = 3.0f,
            Linear = 0.09f,
            Quadratic = 0.032f
        });

        // Run the simulation
        world.Run();
    }
}
