using System.Numerics;
using Lab3.Asteroids;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Lighting;
using SilkOpenGL.Store;

class Program
{
    public const string LineShaderName = "LineShader";

    public static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Multiple Shapes Scene";

        StoreManager storeManager = new StoreManager();
        storeManager.AddShader(LineShaderName, "Shaders/line.vert", "Shaders/line.frag");

        World world = new World(options, storeManager);
        world.AddObject(new LightEntity
        {
            Position = new Vector3(0f, 0f, 1.2f),
            Ambient = new Vector3(0.15f, 0.15f, 0.2f),
            Diffuse = new Vector3(1f, 0.95f, 0.85f),
            Intensity = 1.1f,
            Linear = 0.14f,
            Quadratic = 0.12f
        });

        AsteroidsGame game = new AsteroidsGame(world);
        world.AddObject(game);
        world.Run();
    }
}