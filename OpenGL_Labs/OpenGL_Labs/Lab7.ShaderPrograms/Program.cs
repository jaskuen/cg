using System.Numerics;
using Lab7.ShaderPrograms.Tasks;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkOpenGL;
using SilkOpenGL.Camera;
using SilkOpenGL.Lighting;
using SilkOpenGL.Store;

internal static class Program
{
    public const string CanabolaShaderName = "CanabolaShader";
    public const string ChinaFlagShaderName = "ChinaShader";
    public const string MorphShaderName = "MorphShader";
    public const string RippleShaderName = "RippleShader";
    public const string RippleSourceTextureName = "RippleSource";
    public const string RippleTargetTextureName = "RippleTarget";

    public static void Main( string[] args )
    {
        Directory.SetCurrentDirectory( AppContext.BaseDirectory );

        string task = "3";

        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>( 1280, 720 );
        options.Title = "Lab 7";

        StoreManager storeManager = new();
        switch ( task )
        {
            case "1":
                storeManager.AddShader( CanabolaShaderName, "Shaders/canabola.vert", "Shaders/canabola.frag" );
                break;
            case "2":
                storeManager.AddShader( ChinaFlagShaderName, "Shaders/china.vert", "Shaders/china.gsh", "Shaders/china.frag" );
                break;
            case "3":
                storeManager.AddShader( MorphShaderName, "Shaders/morph.vert", "Shaders/morph.frag" );
                break;
            default:
                storeManager.AddShader( RippleShaderName, "Shaders/ripple.vert", "Shaders/ripple.frag" );
                storeManager.AddTexture( RippleSourceTextureName, "Images/ripple-source.png" );
                storeManager.AddTexture( RippleTargetTextureName, "Images/ripple-target.png" );
                break;
        }

        CameraObject camera = new()
        {
            Position = task == "3" || task == "4"
                ? new Vector3( 0f, 0f, 3.2f )
                : new Vector3( 0f, 0f, 2.6f )
        };

        if ( task == "3" )
        {
            camera.SetMode( CameraMode.Rotate );
        }
        else
        {
            camera.SetViewPoint( Vector3.Zero );
        }

        World world = new( options, storeManager, camera, task == "3", task == "3" );
        switch ( task )
        {
            case "1":
                world.AddObject( new Task1() );
                break;
            case "2":
                world.AddObject( new Task2() );
                break;
            case "3":
                world.AddLight( new LightEntity( new Vector3( 1.4f, 1.2f, 2.2f ), new Vector3( 1.0f, 0.9f, 0.78f ) )
                {
                    Ambient = new Vector3( 0.22f ),
                    Specular = new Vector3( 0.45f ),
                    Intensity = 2.2f,
                    Linear = 0.09f,
                    Quadratic = 0.032f
                } );
                world.AddObject( new Task3() );
                break;
            default:
                world.AddObject( new Task4( RippleSourceTextureName, RippleTargetTextureName ) );
                break;
        }

        world.Run();
    }
}
