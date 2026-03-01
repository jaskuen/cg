using System.Drawing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkOpenGL.Objects;
using SilkOpenGL.Store;

namespace SilkOpenGL;

public class World
{
    private IWindow _window;
    private GL _gl;
    private ShaderStore _shaderStore;
    private TextureStore _textureStore; // Аналогично ShaderStore, если нужно кэшировать текстуры
    private ObjectManager _objectManager;
    private Camera _camera;

    public World(IWindow window, ShaderStore shaderStore, TextureStore textureStore)
    {
        _shaderStore = shaderStore;
        _textureStore = textureStore;
        _objectManager = new ObjectManager(_shaderStore, _textureStore);
        _camera = new Camera();

        _window = window;
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Closing += OnUnload;
    }

    private void OnLoad()
    {
        _gl = _window.CreateOpenGL();

        CompileShadersAndTextures();

        AddCameraMove();

        // Инициализация глобальных ресурсов, если нужно
    }

    private void OnUpdate(double dt)
    {
        _objectManager.Update(_gl, dt); // Обновляет все объекты
    }

    private unsafe void OnRender(double dt)
    {
        _gl.ClearColor(Color.Chartreuse);
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

        _objectManager.Render(_gl, dt); // Рисует все объекты
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        _gl.Viewport(size);
    }

    private void OnUnload()
    {
        _objectManager.DisposeAll();
        _shaderStore.Dispose();
        _textureStore.Dispose();
        // Очистка GL
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

        // mouse.Cursor.CursorMode = CursorMode.Raw; // для FPS-стиля
        // mouse.MouseMove += (_, delta) => _camera.ProcessMouseMove(mouse, delta);
        mouse.MouseMove += (_, delta) => Console.WriteLine(delta);
            
        keyboard.KeyDown += (_, _, _) => _camera.ProcessKeyboard(keyboard, 0.05f);
    }

    // Публичные методы для добавления/удаления
    public void AddObject(RenderableObject obj) => _objectManager.Add(obj);
    public void RemoveObject(RenderableObject obj) => _objectManager.Remove(obj);

    public void Run() => _window.Run();
}