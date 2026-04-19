using System.Numerics;
using Lab5.Tiles;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Store;

class Program
{
    public const string TileShaderName = "TileShader";
    public static List<string> Textures = [];

    public static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Multiple Shapes Scene";

        StoreManager storeManager = new StoreManager();
        storeManager.AddShader(TileShaderName, "Shaders/shader.vert", "Shaders/shader.frag");
        AddTextures(storeManager);

        World world = new World(options, storeManager, null);
        
        Game game = new Game(world, Textures, TileShaderName, GameMode.Normal);
        
        world.AddObject(game);
        world.Run();
    }

    private static void AddTextures(StoreManager storeManager)
    {
        string imagesDir = Path.Combine(AppContext.BaseDirectory, "Images");
        if (!Directory.Exists(imagesDir))
        {
            imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "Images");
        }

        if (!Directory.Exists(imagesDir))
        {
            throw new DirectoryNotFoundException($"Images folder not found: {imagesDir}");
        }

        HashSet<string> allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".webp", ".bmp"
        };

        var imagePaths = Directory
            .GetFiles(imagesDir)
            .Where(path => allowedExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (imagePaths.Count == 0)
        {
            throw new InvalidOperationException($"No supported image files found in: {imagesDir}");
        }

        foreach (string imagePath in imagePaths)
        {
            Textures.Add(storeManager.AddTextureAndGetKey(Guid.NewGuid().ToString(), imagePath));
        }
    }
}