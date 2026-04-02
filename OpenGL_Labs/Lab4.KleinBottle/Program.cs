// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Lab4.KleinBottle.Objects;
using Silk.NET.Assimp;
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
        options.Title = "Multiple Shapes Scene";

        StoreManager storeManager = new StoreManager();
        storeManager.AddShader(StellationShaderName, "Shaders/stellation.vert", "Shaders/stellation.frag");

        World world = new World(options, storeManager, CameraMode.Rotate);
        world.AddObject(new LightEntity
        {
            Position = new Vector3(0f, 0.5f, 1f),
            Ambient = new Vector3(0.25f, 0.25f, 0.35f),
            Diffuse = new Vector3(0.8f, 0.9f, 1f),
            Intensity = 1.2f,
            Linear = 0.12f,
            Quadratic = 0.1f
        });

        KleinBottle bottle = new KleinBottle(StellationShaderName);
        
        world.AddObject(bottle);
        world.Run();
    }
}