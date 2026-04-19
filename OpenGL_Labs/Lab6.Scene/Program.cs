// See https://aka.ms/new-console-template for more information

using System.Numerics;
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

        World world = new World(options, storeManager, CameraMode.Rotate);

        ModelData ballData = ModelLoader.Load("Models/volleyball.obj");
        ModelObject ball = new ModelObject(ShaderName, ballData, BallMaterial, true);
        ball.Transform.Scale = new Vector3(0.05f);
        ball.Transform.Position = new Vector3(0f, -1f, 0f);

        ModelData fieldData = ModelLoader.Load("Models/field.obj");
        ModelObject field = new ModelObject(ShaderName, fieldData);
        field.Transform.Position = new Vector3(0, -3f, 0);

        world.AddLight(new LightEntity(new Vector3(10f, 10f, 10f), new Vector3(2.2f)));

        world.AddObject(ball);
        world.AddObject(field);
        // world.AddObject(car);
        world.Run();
    }
}