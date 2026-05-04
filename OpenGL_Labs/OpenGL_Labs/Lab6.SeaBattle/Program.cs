using System.Numerics;
using Lab6.SeaBattle.Game;
using Lab6.SeaBattle.Rendering;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Camera;
using SilkOpenGL.Lighting;
using SilkOpenGL.Store;

namespace Lab6.SeaBattle;

internal static class Program
{
    public const string BasicShader = "Basic";
    public const string SkyShader = "Sky";

    private static void Main() 
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Sea Battle 3D";

        StoreManager stores = new();
        stores.AddShader(BasicShader, "Shaders/basic.vert", "Shaders/basic.frag");
        stores.AddShader(SkyShader, "Shaders/sky.vert", "Shaders/sky.frag");

        CameraObject camera = new()
        {
            Position = new Vector3(0f, 4.6f, 0f),
            NearPlane = 0.1f,
            FarPlane = 120f
        };

        World world = new(options, stores, camera);
        world.AddLight(new LightEntity(new Vector3(-8f, 14f, 7f), new Vector3(1f, 0.95f, 0.82f))
        {
            Ambient = new Vector3(0.22f, 0.25f, 0.3f),
            Intensity = 4.2f,
            Linear = 0.035f,
            Quadratic = 0.006f
        });
        world.AddLight(new LightEntity(new Vector3(12f, 7f, -20f), new Vector3(0.35f, 0.55f, 0.85f))
        {
            Ambient = new Vector3(0.04f, 0.07f, 0.1f),
            Intensity = 1.4f,
            Linear = 0.045f,
            Quadratic = 0.009f
        });

        SeaBattleGame game = new(world);
        AimBackdrop aimBackdrop = new(SkyShader);
        world.AddObject(aimBackdrop);
        game.Start();
        world.AddObject(game);

        world.Run();
    }
}
