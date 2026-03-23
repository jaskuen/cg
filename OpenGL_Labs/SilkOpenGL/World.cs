using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkOpenGL.Lighting;
using SilkOpenGL.Objects;
using SilkOpenGL.Store;
using CameraType = SilkOpenGL.Camera.Camera;

namespace SilkOpenGL;

public class World
{
    private IWindow _window;
    private GL _gl;
    private ShaderStore _shaderStore;
    private TextureStore _textureStore;
    private FontStore _fontStore;
    private MaterialStore _materialStore;
    private ObjectManager _objectManager;
    private CameraType _camera;
    private PickingService _pickingService;

    private IMouse? _mouse;
    private IKeyboard? _keyboard;

    private readonly List<IKeyboardClickable> _keyboardClickables = [];
    private readonly List<LightEntity> _lights = [];

    private IClickable? _lastActive;

    private bool _useMouseCameraMove = true;
    private readonly CameraMode _cameraMode;

    public World(
        WindowOptions windowOptions,
        ShaderStore shaderStore,
        TextureStore textureStore,
        FontStore fontStore,
        MaterialStore materialStore,
        CameraMode cameraMode = CameraMode.Fps )
    {
        _shaderStore = shaderStore;
        _textureStore = textureStore;
        _fontStore = fontStore;
        _materialStore = materialStore;
        _cameraMode = cameraMode;
        _objectManager = new ObjectManager( _shaderStore, _textureStore, _fontStore, _materialStore );
        _camera = new CameraType();
        _camera.SetMode( _cameraMode, Vector3.Zero );
        _pickingService = new PickingService( windowOptions.Size.X, windowOptions.Size.Y );

        _window = Window.Create( windowOptions );
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Closing += OnUnload;
    }

    public World( WindowOptions windowOptions, StoreManager storeManager, CameraMode cameraMode = CameraMode.Fps )
        : this( windowOptions, storeManager.ShaderStore, storeManager.TextureStore, storeManager.FontStore, storeManager.MaterialStore, cameraMode )
    {
    }

    private void OnLoad()
    {
        _gl = _window.CreateOpenGL();

        _pickingService.SetupFramebuffer( _gl );

        _gl.Enable( EnableCap.Blend );
        _gl.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );

        RegisterShaders();
        CompileShadersAndTextures();

        AddInputContext();

        // Инициализация глобальных ресурсов, если нужно
    }

    private void OnUpdate( double dt )
    {
        _objectManager.Update( _gl, dt );
    }

    private unsafe void OnRender( double dt )
    {
        _gl.ClearColor( Color.DarkSlateGray );
        _gl.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );

        foreach ( var kv in _shaderStore.AllShaders )
        {
            kv.Value.Use();
            int viewLoc = _gl.GetUniformLocation( kv.Value.ProgramId, "uView" );
            int projLoc = _gl.GetUniformLocation( kv.Value.ProgramId, "uProjection" );

            var view = _camera.ViewMatrix;
            var proj = _camera.ProjectionMatrix( ( float )_window.Size.X / _window.Size.Y );

            _gl.UniformMatrix4( viewLoc, 1, false, ( float* )&view );
            _gl.UniformMatrix4( projLoc, 1, false, ( float* )&proj );

            ApplyLights( kv.Value );
        }

        _objectManager.Render( _gl, dt );
    }

    private void OnFramebufferResize( Vector2D<int> size )
    {
        _gl.Viewport( size );
        _pickingService.UpdateViewport( size.X, size.Y );
    }

    private void OnUnload()
    {
        _objectManager.DisposeAll();
        _shaderStore.Dispose();
        _textureStore.Dispose();
    }

    private void CompileShadersAndTextures()
    {
        foreach ( Shader shader in _shaderStore.AllShaders.Values )
        {
            shader.Compile( _gl );
        }

        foreach ( Texture texture in _textureStore.AllTextures.Values )
        {
            texture.Compile( _gl );
        }

        InitTextureHandlesBuffer();
    }

    private unsafe void InitTextureHandlesBuffer()
    {
        List<ulong> handles = [];

        var sortedTextures = _textureStore.AllTextures.Values.OrderBy(x => x.TextureId).ToList();
        handles.AddRange( sortedTextures.Select( x => x.BindlessHandle ) );

        ulong[] textureHandles = handles.ToArray();

        uint ssbo = _gl.GenBuffer();
        _gl.BindBuffer( BufferTargetARB.ShaderStorageBuffer, ssbo );

        fixed ( ulong* ptr = textureHandles )
        {
            _gl.BufferData( BufferTargetARB.ShaderStorageBuffer,
                ( nuint )( textureHandles.Length * sizeof( ulong ) ),
                ptr,
                BufferUsageARB.StaticDraw );
        }

        _gl.BindBufferBase( BufferTargetARB.ShaderStorageBuffer, 0, ssbo );
    }

    private void AddInputContext()
    {
        IInputContext inputContext = _window.CreateInput();

        var mouse = inputContext.Mice.First();
        _mouse = mouse;

        var keyboard = inputContext.Keyboards.First();
        _keyboard = keyboard;

        RegisterInputObjects();

        if ( _useMouseCameraMove )
        {
            if ( _cameraMode == CameraMode.Fps )
            {
                mouse.Cursor.CursorMode = CursorMode.Raw;
            }

            mouse.MouseMove += ( _, delta ) => _camera.ProcessMouseMove( mouse, delta );
        }

        mouse.MouseUp += ( _, _ ) => PerformMouseAction( mouse, MouseAction.Up );
        mouse.MouseDown += ( _, _ ) => PerformMouseAction( mouse, MouseAction.Down );
        mouse.MouseMove += ( _, _ ) => PerformMouseAction( mouse, MouseAction.Move );

        // mouse.MouseMove += (_, delta) => Console.WriteLine(delta);

        _window.Update += dt => _camera.ProcessKeyboard(keyboard, dt);
    }

    private void RegisterInputObjects()
    {
        foreach ( IKeyboardClickable keyboardClickable in _keyboardClickables )
        {
            keyboardClickable.Keyboard = _keyboard!;
        }
    }

    private void RegisterShaders()
    {
        _shaderStore.CreateShader( "picking", "./Picking/picking.vert", "./Picking/picking.frag" );
        _shaderStore.CreateShader( "text", "./Text/text.vert", "./Text/text.frag" );
        _textureStore.CreateTexture( "text", "./Text/font.png" );
        _fontStore.CreateFont( "font", "./Text/font.xml" );
    }

    private uint PerformMouseAction( IMouse mouse, MouseAction action )
    {
        Vector2 mousePos = mouse.Position;

        if ( _useMouseCameraMove && _cameraMode == CameraMode.Fps )
        {
            mousePos = new Vector2( ( float )_window.Size.X / 2, ( float )_window.Size.Y / 2 );
        }

        // Console.WriteLine($"{clickedPosition.X}, {clickedPosition.Y}");

        DrawPickingTextures();

        uint clickedId = _pickingService.ReadIdAt( ( int )mousePos.X, ( int )mousePos.Y );

        if ( clickedId != 0 )
        {
            var target = _objectManager.Objects
                .OfType<IClickable>()
                .FirstOrDefault( x => x.ColorId == clickedId );

            if ( target != null )
            {
                if ( _lastActive == null )
                {
                    _lastActive = target;
                    target.OnMouseEnter();
                }
                else if ( _lastActive.ColorId != clickedId )
                {
                    _lastActive?.OnMouseLeave();
                    _lastActive = target;
                    target.OnMouseEnter();
                }

                // Получаем мировые координаты объекта
                float objectZ = ( target as RenderableObject )?.Position.Z ?? 0;
                Vector3 worldPos =
                    _camera.Unproject( mousePos, new Vector2( _window.Size.X, _window.Size.Y ), objectZ );

                switch ( action )
                {
                    case MouseAction.Up: target.OnMouseUp( worldPos ); break;
                    case MouseAction.Down: target.OnMouseDown( worldPos ); break;
                    case MouseAction.Move: target.OnMouseMove( worldPos ); break;
                }
            }
        }
        else
        {
            _lastActive?.OnMouseLeave();
            _lastActive = null;
        }

        return clickedId;
    }

    private void DrawPickingTextures()
    {
        // 1. Очищаем FBO для пикинга
        _pickingService.BindForRendering();
        _gl.ClearColor( 0, 0, 0, 0 );
        _gl.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );

        var pickingShader = _shaderStore.GetShader( "picking" );
        pickingShader.Use();

        // Передаем общие матрицы камеры
        pickingShader.SetUniform( "uView", _camera.ViewMatrix );
        pickingShader.SetUniform( "uProjection", _camera.ProjectionMatrix( ( float )_window.Size.X / _window.Size.Y ) );

        // 2. Рендерим только кликабельные объекты
        foreach ( var obj in _objectManager.Objects )
        {
            if ( obj is IClickable clickable )
            {
                Vector3 colorId = _pickingService.IdToColor( clickable.ColorId );
                pickingShader.SetUniform( "uPickingColor", colorId );
                obj.OnRenderPicking( _gl, pickingShader );
            }
        }

        // Возвращаемся к обычному буферу экрана
        _gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
    }

    private void ApplyLights( Shader shader )
    {
        var activeLights = _lights.Where( x => x.Enabled ).Take( 8 ).ToList();
        shader.TrySetUniform( "uLightCount", activeLights.Count );

        for ( int i = 0; i < activeLights.Count; i++ )
        {
            LightEntity light = activeLights[i];
            string prefix = $"uLights[{i}]";

            shader.TrySetUniform( $"{prefix}.position", light.Position );
            shader.TrySetUniform( $"{prefix}.ambient", light.Ambient );
            shader.TrySetUniform( $"{prefix}.diffuse", light.Diffuse );
            shader.TrySetUniform( $"{prefix}.specular", light.Specular );
            shader.TrySetUniform( $"{prefix}.intensity", light.Intensity );
            shader.TrySetUniform( $"{prefix}.constant", light.Constant );
            shader.TrySetUniform( $"{prefix}.linear", light.Linear );
            shader.TrySetUniform( $"{prefix}.quadratic", light.Quadratic );
        }
    }

    // Публичные методы для добавления/удаления
    public void AddObject( UpdateableObject obj )
    {
        if ( obj is LightEntity lightEntity )
        {
            _lights.Add( lightEntity );
        }

        if ( obj is RenderableObject renderable )
        {
            _objectManager.Add( renderable );
        }
        else
        {
            _window.Update += obj.OnUpdate;
        }


        if ( obj is IClickable clickable )
        {
            _pickingService.Register( clickable );
        }

        if ( obj is IKeyboardClickable keyboardClickable )
        {
            if ( _keyboard is null )
            {
                _keyboardClickables.Add( keyboardClickable );
            }
            else
            {
                keyboardClickable.Keyboard = _keyboard;
            }
        }
    }

    public void RemoveObject( RenderableObject obj ) => _objectManager.Remove( obj );

    public void AddLight( LightEntity light )
    {
        _lights.Add( light );
    }

    public bool RemoveLight( LightEntity light )
    {
        return _lights.Remove( light );
    }

    public void Run() => _window.Run();
}