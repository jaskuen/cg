using System.Numerics;
using Lab4.Dodecahedron.Objects;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Lighting;
using SilkOpenGL.Store;

class Program
{
    public const string StellationShaderName = "StellationShader";

    public static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Lab4 - Third Stellation of Dodecahedron";

        StoreManager storeManager = new StoreManager();
        storeManager.AddShader(StellationShaderName, "Shaders/stellation.vert", "Shaders/stellation.frag");

        World world = new World(options, storeManager, CameraMode.Rotate);

        world.AddObject(new LightEntity
        {
            Position = new Vector3(2.0f, 1.5f, 2.5f),
            Ambient = new Vector3(0.12f, 0.12f, 0.16f),
            Diffuse = new Vector3(1.0f, 0.95f, 0.85f),
            Intensity = 1.4f,
            Linear = 0.08f,
            Quadratic = 0.03f
        });

        world.AddObject(new LightEntity
        {
            Position = new Vector3(-2.4f, -1.2f, 1.8f),
            Ambient = new Vector3(0.05f, 0.07f, 0.12f),
            Diffuse = new Vector3(0.45f, 0.65f, 1.0f),
            Intensity = 0.9f,
            Linear = 0.12f,
            Quadratic = 0.08f
        });

        world.AddObject(new ThirdStellationDodecahedron(StellationShaderName));
        world.Run();
    }
}
