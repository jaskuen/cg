using Lab3.FishTank;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
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

        Scene scene = new Scene(world);
        
        world.AddObject(scene);
        world.Run();
    }
}

