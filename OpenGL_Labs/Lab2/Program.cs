using System.Drawing;
using System.Numerics;
using Lab2;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Store;

class Program
{
    private const string TileShaderName = "ColorShader";
    private const string CircleShaderName = "CircleShader";
    private const string TexturedCircleShaderName = "TexturedCircleShader";

    private const string BallTextureName = "BallTexture";

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Multiple Shapes Scene";

        using var window = Window.Create(options);

        ShaderStore shaderStore = new ShaderStore();
        shaderStore.CreateShader(TileShaderName, "shader.vert", "shader.frag");
        shaderStore.CreateShader(CircleShaderName, "circle.vert", "circle.frag");
        shaderStore.CreateShader(TexturedCircleShaderName, "circle_with_texture.vert", "circle_with_texture.frag");

        TextureStore textureStore = new TextureStore();
        textureStore.CreateTexture(BallTextureName, "ball.png");

        var world = new World(window, shaderStore, textureStore);

        // Создаём и добавляем несколько кубов
        world.AddObject(new Tile(new Vector2D<float>(-0.8f, -0.8f), TileShaderName));
        world.AddObject(new Tile(new Vector2D<float>(0.4f, 0.4f), TileShaderName));
        world.AddObject(new Tile(new Vector2D<float>(0.4f, -0.4f), TileShaderName));
        world.AddObject(new Circle(new Vector2(-0.3f, 0.3f), 0.5f, Color.Brown, TexturedCircleShaderName,
            BallTextureName));

        world.Run();
    }
}