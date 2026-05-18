using System.Drawing;
using System.Numerics;
using Lab8;
using Lab8.Primitives;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Camera;
using SilkOpenGL.Helpers;
using SilkOpenGL.Lighting;
using SilkOpenGL.RayTracing;
using SilkOpenGL.Store;

internal static class Program
{
    public const string ObjectShaderName = "Object";

    public static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1024, 768);
        options.Title = "Lab 8";

        CameraObject camera = new()
        {
            Position = new Vector3(0f, 1.7f, 1.2f),
            NearPlane = 0.1f,
            FarPlane = 100f
        };
        camera.SetViewPoint(new Vector3(0f, -0.1f, -1.5f));

        StoreManager storeManager = new();
        storeManager.AddShader(ObjectShaderName, "Shaders/object.vert", "Shaders/object.frag");

        World world = new(options, storeManager, camera, true, true)
        {
            RenderMode = RenderMode.RayTracing
        };

        world.RayTracingSettings.RenderWidth = 1024;
        world.RayTracingSettings.RenderHeight = 768;
        world.RayTracingSettings.MaxReflectionDepth = 2;

        world.AddLight(new LightEntity(new Vector3(2.5f, 4f, 2.2f), new Vector3(1f, 0.92f, 0.82f))
        {
            Ambient = new Vector3(0.18f),
            Specular = new Vector3(0.75f),
            Intensity = 4.5f,
            Linear = 0.07f,
            Quadratic = 0.017f
        });

        world.AddLight(new LightEntity(new Vector3(-3f, 2.2f, 0f), new Vector3(0.45f, 0.62f, 1f))
        {
            Ambient = new Vector3(0.04f),
            Specular = new Vector3(0.35f),
            Intensity = 1.2f,
            Linear = 0.14f,
            Quadratic = 0.07f
        });

        RayMaterial torusMaterial = new RayMaterial(
            new Vector3(0.22f, 0.82f, 0.56f),
            new Vector3(0.08f),
            new Vector3(0.88f),
            new Vector3(0.9f),
            96f,
            0.18f);
        
        world.AddObject(new RayTracedTorus(
            new Vector3(0.55f, 0.05f, -2.2f),
            0.72f,
            0.22f, torusMaterial));
        
        world.AddObject(new RayTracedTorus(
            new Vector3(0.55f, 0.45f, -2.2f),
            0.62f,
            0.22f, torusMaterial with{ BaseColor = ColorHelper.ColorToVector3(Color.Red) }));
        
        world.AddObject(new RayTracedTorus(
            new Vector3(0.55f, 0.85f, -2.2f),
            0.52f,
            0.22f, torusMaterial with{ BaseColor = ColorHelper.ColorToVector3(Color.Blue) }));
        
        world.AddObject(new RayTracedTorus(
            new Vector3(0.55f, 1.25f, -2.2f),
            0.42f,
            0.22f, torusMaterial with{ BaseColor = ColorHelper.ColorToVector3(Color.WhiteSmoke) }));

        world.AddObject(new RayTracedPlane(
            new Vector3(0f, -0.82f, 0f),
            Vector3.UnitY,
            new RayMaterial(
                new Vector3(0.58f, 0.6f, 0.56f),
                new Vector3(0.1f),
                new Vector3(0.75f),
                new Vector3(0.25f),
                18f,
                0.2f)));
        
        Lab8Scene scene = new(world);
        world.AddObject(scene);

        world.Run();
    }
}
