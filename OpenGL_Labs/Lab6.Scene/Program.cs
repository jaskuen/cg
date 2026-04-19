// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Lab6.Scene;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
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

        World world = new World(options, storeManager, CameraMode.Fps);

        ModelData ballData = ModelLoader.Load("Models/volleyball.obj");
        ModelObject ball = new ModelObject(ShaderName, ballData, BallMaterial, true);
        ball.Transform.Scale = new Vector3(0.05f);
        ball.Transform.Position = new Vector3(3f, 1f, 0f);
        ModelObject ball2 = new ModelObject(ShaderName, ballData, BallMaterial, true);
        ball2.Transform.Scale = new Vector3(0.1f);
        ball2.Transform.Position = new Vector3(-3f, 1f, 0f);

        // ModelData cubeData = ModelLoader.Load("Models/Cube.fbx");
        // for (int i = 0; i < 10; i++)
        // {
        //     ModelObject cube = new ModelObject(ShaderName, cubeData, BallMaterial, true);
        //     cube.Transform.Scale = new Vector3(0.1f);
        //     cube.Transform.Position = new Vector3(3f, 5f - i * 0.5f, 0f);
        //     world.AddObject(cube);
        // }

        ModelData fieldData = ModelLoader.Load("Models/Field.fbx");
        ModelObject field = new ModelObject(ShaderName, fieldData);
        // field.Transform.Position = new Vector3(0, -3f, 0);
        // field.Transform.Scale = new Vector3(0.1f);

        // ModelData bottlesData = ModelLoader.Load("Models/beer-bottle-carrier.obj");
        // ModelObject btls = new ModelObject(ShaderName, bottlesData);
        // btls.Transform.Position = new Vector3(0, -0.1f, 0);

        world.AddLight(new LightEntity(new Vector3(3f, 3f, 3f), new Vector3(2.2f)));

        // world.AddObject(btls);
        world.AddObject(field);
        // world.AddObject(ball2);
        // Scene scene = new Scene(world, field);
        // world.AddObject(scene);
        // world.AddObject(ball);
        // world.AddObject(car);
        world.Run();
    }
}