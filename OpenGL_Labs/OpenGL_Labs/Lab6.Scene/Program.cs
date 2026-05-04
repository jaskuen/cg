// See https://aka.ms/new-console-template for more information

using System.Numerics;
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

    public static void Main( string[] args )
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>( 1280, 720 );
        options.Title = "Bugatti Scene";

        StoreManager storeManager = new StoreManager();
        storeManager.AddShader( ShaderName, "Shaders/shader.vert", "Shaders/shader.frag" );

        CameraObject camera = new CameraObject();
        camera.Position = new Vector3( 0f, 18f, 11f );
        camera.NearPlane = 0.5f;
        camera.FarPlane = 400.0f;
        camera.SetMode( CameraMode.Rotate );

        World world = new World( options, storeManager, camera, true );

        world.AddLight( new LightEntity( new Vector3( 10f, 10f, 10f ), new Vector3( 2.2f ) ) );
        world.AddLight( new LightEntity( new Vector3( -30f, 25f, 40f ), new Vector3( 1.6f ) ) );

        ModelData carData = ModelLoader.Load(
            "Models/bugatti.obj",
            new ModelImportOptions
            {
                GenerateSmoothNormals = true,
                IncludeTriangles = true,
                IncludeLines = false,
                IncludePoints = false,
                JoinIdenticalVertices = false,
                // ExcludedMeshNameSubstrings = [ "alights", "NurbsPath", "Bezier", "Text", "Plane.014_Plane.020" ],
                MaxMeshExtent = 20.0f,
                MinimumPrimitiveCount = 100
            } );
        ModelObject car = new ModelObject( ShaderName, carData );
        car.Transform.Scale = new Vector3( 3.0f );
        car.Transform.Position = new Vector3( 0f, -2.5f, 0f );

        world.AddObject( car );
        world.Run();
    }
}
