// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Lab6.Scene;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Camera;
using SilkOpenGL.Lighting;
using SilkOpenGL.Model;
using SilkOpenGL.Store;

public class Program
{
    public const string ShaderName = "Shader";
    public const string BallMaterial = "BallMaterial";

    public static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Multiple Shapes Scene";

        StoreManager storeManager = new StoreManager();
        storeManager.AddShader(ShaderName, "Shaders/shader.vert", "Shaders/shader.frag");
        storeManager.AddMaterial(BallMaterial, "Textures/ball/ball.json");

        CameraObject camera = new CameraObject
        {
            Position = new Vector3(0f, 12f, 30f),
            NearPlane = 0.1f,
            FarPlane = 200f
        };
        camera.SetMode(CameraMode.Fps, new Vector3(0f, 1f, 0f));

        World world = new World(options, storeManager, camera, true, true);

        ModelData ballData = ModelLoader.Load("Models/volleyball.obj");
        ModelObject ball = new ModelObject(ShaderName, ballData, BallMaterial, true);
        ball.Transform.Scale = new Vector3(0.01f);
        ball.Transform.Position = new Vector3(3f, 0.55f, 0f);
        ModelObject ball2 = new ModelObject(ShaderName, ballData, BallMaterial, true);
        ball2.Transform.Scale = new Vector3(0.01f);

        ModelData fieldData = ModelLoader.Load("Models/field.obj");
        ModelObject field = new ModelObject(ShaderName, fieldData);
        field.Transform.Scale = Vector3.One;
        
        ModelData canonData = ModelLoader.Load("Models/canon.glb");
        ModelObject canon = new ModelObject(ShaderName, canonData);
        canon.Transform.Scale = new Vector3(0.08f);
        canon.Transform.Position = new Vector3(-3f, 0.5f, 20f);
        
        ModelData kageyamaData = ModelLoader.Load("Models/kageyama.glb");
        ModelObject kageyama = new ModelObject(ShaderName, kageyamaData);
        kageyama.Transform.Scale = new Vector3(0.04f);

        Scene scene = new Scene(world, ball2, kageyama);

        world.AddLight(new LightEntity(new Vector3(0f, 18f, 12f), new Vector3(1.4f)));
        world.AddLight(new LightEntity(new Vector3(-18f, 12f, -18f), new Vector3(0.8f)));

        world.AddObject(field);
        world.AddObject(ball);
        world.AddObject(ball2);
        world.AddObject(canon);
        world.AddObject(kageyama);
        world.AddObject(scene);
        world.Run();
    }
}
