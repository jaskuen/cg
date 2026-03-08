using Silk.NET.OpenGL;
using SilkOpenGL.Store;

namespace SilkOpenGL.Objects;

public class ObjectManager
{
    private readonly List<RenderableObject> _objects = new();
    private readonly List<RenderableObject> _toAdd = new();
    private readonly List<RenderableObject> _toRemove = new();

    private readonly ShaderStore _shaderStore;
    private readonly TextureStore _textureStore;

    // Для оптимизации: группировка по шейдерам (опционально, добавь позже)
    private Dictionary<string, List<RenderableObject>> _objectsByShader = new();

    public ObjectManager(ShaderStore shaderStore, TextureStore textureStore)
    {
        _shaderStore = shaderStore;
        _textureStore = textureStore;
    }

    public void Add(RenderableObject obj)
    {
        _toAdd.Add(obj); // Отложенное добавление — безопасно из любого потока
    }

    public void Remove(RenderableObject obj)
    {
        _toRemove.Add(obj);
    }

    public void Update(GL gl, double dt)
    {
        ProcessPending(gl); // Сначала добавляем/удаляем

        foreach (var obj in _objects)
        {
            obj.OnUpdate(dt);
        }
    }

    public void Render(GL gl, double dt)
    {
        // Вариант 1: Простой foreach
        // foreach (var obj in _objects) obj.OnRender(gl, dt);

        // Вариант 2: Оптимизированный по шейдерам (группировка)
        var groups = _objects.GroupBy(obj => obj.ShaderKey); // Или используй _objectsByShader
        foreach (var group in groups)
        {
            Shader shader = _shaderStore.GetShader(group.Key);
            shader.Use(); // Переключаем шейдер один раз на группу

            foreach (var obj in group)
            {
                obj.BindResources(); // Bind текстуры, uniforms
                obj.OnRender(dt); // Только draw call
            }
        }
    }
    
    public List<RenderableObject> Objects => _objects;

    private void ProcessPending(GL gl)
    {
        foreach (var obj in _toAdd)
        {
            obj.Init(_shaderStore, _textureStore, gl); // Инициализация ресурсов
            _objects.Add(obj);

            // Если используешь группировку:
            // if (!_objectsByShader.ContainsKey(obj.ShaderKey)) _objectsByShader[obj.ShaderKey] = new();
            // _objectsByShader[obj.ShaderKey].Add(obj);
        }

        _toAdd.Clear();

        foreach (var obj in _toRemove)
        {
            obj.OnClose(); // Очистка ресурсов
            _objects.Remove(obj);
            // _objectsByShader[obj.ShaderKey]?.Remove(obj);
        }

        _toRemove.Clear();
    }

    public void DisposeAll()
    {
        foreach (var obj in _objects)
        {
            obj.OnClose();
        }

        _objects.Clear();
    }
}