using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Lighting;
using SilkOpenGL.Store;

namespace Bhop;

class Program
{
    static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Title = "Bhop";
        options.Size = new Vector2D<int>(1280, 720);

        StoreManager storeManager = new StoreManager();

        storeManager.AddShader("TileShader", "Shaders/shader.vert", "Shaders/shader.frag");

        PlayerObject player = new PlayerObject();

        World world = new World(options, storeManager, player);

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

        Game game = new Game(world, player, map);

        world.AddObject(game);

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