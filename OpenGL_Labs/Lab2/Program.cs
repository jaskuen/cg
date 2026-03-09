using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Store;

namespace Lab2;

class Program
{
    public const string TileShaderName = "TileShader";
    public const string CircleShaderName = "CircleShader";
    public const string TexturedCircleShaderName = "TexturedCircleShader";

    public const string BallTextureName = "BallTexture";
    public const string TileTextureName = "TileTexture";

    static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Multiple Shapes Scene";

        ShaderStore shaderStore = new ShaderStore();
        shaderStore.CreateShader(TileShaderName, "shader.vert", "shader.frag");
        shaderStore.CreateShader(CircleShaderName, "circle.vert", "circle.frag");
        shaderStore.CreateShader(TexturedCircleShaderName, "circle_with_texture.vert", "circle_with_texture.frag");

        TextureStore textureStore = new TextureStore();
        textureStore.CreateTexture(BallTextureName, "ball.png");
        textureStore.CreateTexture(TileTextureName, "tile.jpg");

        FontStore fontStore = new FontStore();

        var world = new World(options, shaderStore, textureStore, fontStore);

        // Создаём и добавляем несколько кубов

        GameField gameField = new GameField(world);
        world.AddObject(gameField);

        world.Run();
    }
}