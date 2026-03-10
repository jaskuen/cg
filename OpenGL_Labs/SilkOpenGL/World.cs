using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkOpenGL.Helpers;
using SilkOpenGL.Objects;
using SilkOpenGL.Store;

namespace SilkOpenGL;

public class World
{
    private IWindow _window;
    private GL _gl;
    private ShaderStore _shaderStore;
    private TextureStore _textureStore;
    private FontStore _fontStore;
    private ObjectManager _objectManager;
    private Camera _camera;
    private PickingService _pickingService;

    private IClickable? _lastActive;

    public World(WindowOptions windowOptions, ShaderStore shaderStore, TextureStore textureStore, FontStore fontStore)
    {
        _shaderStore = shaderStore;
        _textureStore = textureStore;
        _fontStore = fontStore;
        _objectManager = new ObjectManager(_shaderStore, _textureStore, _fontStore);
        _camera = new Camera();
        _pickingService = new PickingService(windowOptions.Size.X, windowOptions.Size.Y);

        _window = Window.Create(windowOptions);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Closing += OnUnload;
    }

    private void OnLoad()
    {
        _gl = _window.CreateOpenGL();

        _pickingService.SetupFramebuffer(_gl);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        RegisterShaders();
        CompileShadersAndTextures();

        AddCameraMove();
    }

    private void OnUpdate(double dt)
    {
        _objectManager.Update(_gl, dt);
    }

    private unsafe void OnRender(double dt)
    {
        _gl.ClearColor(Color.DarkSlateGray);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        foreach (var kv in _shaderStore.AllShaders)
        {
            kv.Value.Use();
            int viewLoc = _gl.GetUniformLocation(kv.Value.ProgramId, "uView");
            int projLoc = _gl.GetUniformLocation(kv.Value.ProgramId, "uProjection");

            var view = _camera.ViewMatrix;
            var proj = _camera.ProjectionMatrix((float)_window.Size.X / _window.Size.Y);

            _gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
            _gl.UniformMatrix4(projLoc, 1, false, (float*)&proj);
        }

        _objectManager.Render(_gl, dt);
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        _gl.Viewport(size);
        _pickingService.UpdateViewport(size.X, size.Y);
    }

    private void OnUnload()
    {
        _objectManager.DisposeAll();
        _shaderStore.Dispose();
        _textureStore.Dispose();
    }

    private void CompileShadersAndTextures()
    {
        foreach (Shader shader in _shaderStore.AllShaders.Values)
        {
            shader.Compile(_gl);
        }

        foreach (Texture texture in _textureStore.AllTextures.Values)
        {
            texture.Compile(_gl);
        }
    }

    private void AddCameraMove()
    {
        IInputContext inputContext = _window.CreateInput();

        var mouse = inputContext.Mice.First();
        var keyboard = inputContext.Keyboards.First();

        // mouse.Cursor.CursorMode = CursorMode.Raw;
        // mouse.MouseMove += (_, delta) => _camera.ProcessMouseMove(mouse, delta);
        mouse.MouseUp += (_, _) => PerformMouseAction(mouse, MouseAction.Up);
        mouse.MouseDown += (_, _) => PerformMouseAction(mouse, MouseAction.Down);
        mouse.MouseMove += (_, _) => PerformMouseAction(mouse, MouseAction.Move);
        
        // mouse.MouseMove += (_, delta) => Console.WriteLine(delta);

        _window.Update += dt => _camera.ProcessKeyboard(keyboard, dt);
    }

    private void RegisterShaders()
    {
        _shaderStore.CreateShader("picking", "./Picking/picking.vert", "./Picking/picking.frag");
        _shaderStore.CreateShader("text", "./Text/text.vert", "./Text/text.frag");
        _textureStore.CreateTexture("text", "./Text/font.png");
        _fontStore.CreateFont("font", "./Text/font.xml");
    }

    private uint PerformMouseAction(IMouse mouse, MouseAction action)
    {
        Vector2 mousePos = mouse.Position;

        Vector2 clickedPosition =
            CoordinateHelper.FromViewportToNdc(mousePos, new Vector2(_window.Size.X, _window.Size.Y));

        // Console.WriteLine($"{clickedPosition.X}, {clickedPosition.Y}");

        DrawPickingTextures();

        uint clickedId = _pickingService.ReadIdAt((int)mousePos.X, (int)mousePos.Y);

        if (clickedId != 0)
        {
            var target = _objectManager.Objects
                .OfType<IClickable>()
                .FirstOrDefault(x => x.ColorId == clickedId);

            if (target != null)
            {
                if (_lastActive == null)
                {
                    _lastActive = target;
                    target.OnMouseEnter();
                }
                else if (_lastActive.ColorId != clickedId)
                {
                    _lastActive?.OnMouseLeave();
                    _lastActive = target;
                    target.OnMouseEnter();
                }

                // Получаем мировые координаты объекта
                float objectZ = (target as RenderableObject)?.Position.Z ?? 0;
                Vector3 worldPos = _camera.Unproject(mousePos, new Vector2(_window.Size.X, _window.Size.Y), objectZ);
                
                switch (action)
                {
                    case MouseAction.Up: target.OnMouseUp(worldPos); break;
                    case MouseAction.Down: target.OnMouseDown(worldPos); break;
                    case MouseAction.Move: target.OnMouseMove(worldPos); break;
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
        _gl.ClearColor(0, 0, 0, 0); // ID 0 — это "пустота"
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var pickingShader = _shaderStore.GetShader("picking");
        pickingShader.Use();

        // Передаем общие матрицы камеры
        pickingShader.SetUniform("uView", _camera.ViewMatrix);
        pickingShader.SetUniform("uProjection", _camera.ProjectionMatrix((float)_window.Size.X / _window.Size.Y));

        // 2. Рендерим только кликабельные объекты
        foreach (var obj in _objectManager.Objects)
        {
            if (obj is IClickable clickable)
            {
                Vector3 colorId = _pickingService.IdToColor(clickable.ColorId);
                pickingShader.SetUniform("uPickingColor", colorId);
                obj.OnRenderPicking(_gl, pickingShader);
            }
        }

        // Возвращаемся к обычному буферу экрана
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    // Публичные методы для добавления/удаления
    public void AddObject(UpdateableObject obj)
    {
        if (obj is RenderableObject renderable)
        {
            _objectManager.Add(renderable);
        }
        else
        {
            _window.Update += obj.OnUpdate;
        }


        if (obj is IClickable clickable)
        {
            _pickingService.Register(clickable);
        }
    }

    public void RemoveObject(RenderableObject obj) => _objectManager.Remove(obj);

    public void Run() => _window.Run();
}