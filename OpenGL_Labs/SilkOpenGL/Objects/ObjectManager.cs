using Silk.NET.OpenGL;
using SilkOpenGL.Store;
using SilkOpenGL.Text;

namespace SilkOpenGL.Objects;

public class ObjectManager
{
    private readonly List<RenderableObject> _objects = new();
    private readonly List<RenderableObject> _toAdd = new();
    private readonly List<RenderableObject> _toRemove = new();

    private readonly ShaderStore _shaderStore;
    private readonly TextureStore _textureStore;
    private readonly FontStore _fontStore;
    private readonly MaterialStore _materialStore;

    private Dictionary<string, List<RenderableObject>> _objectsByShader = new();

    public ObjectManager(ShaderStore shaderStore, TextureStore textureStore, FontStore fontStore, MaterialStore materialStore)
    {
        _shaderStore = shaderStore;
        _textureStore = textureStore;
        _fontStore = fontStore;
        _materialStore = materialStore;
    }

    public void Add(RenderableObject obj)
    {
        _toAdd.Add(obj);
    }

    public void Remove(RenderableObject obj)
    {
        _toRemove.Add(obj);
        _objects.Remove(obj);
    }

    public void Update(GL gl, double dt)
    {
        ProcessPending(gl);

        foreach (var obj in _objects)
        {
            obj.OnUpdate(dt);
        }
    }

    public void Render(GL gl, double dt)
    {
        var groups = _objects.GroupBy(obj => obj.ShaderKey);
        foreach (var group in groups)
        {
            Shader shader = _shaderStore.GetShader(group.Key);
            shader.Use();

            foreach (var obj in group)
            {
                if (obj.ClearDepthBeforeRender)
                {
                    gl.Clear(ClearBufferMask.DepthBufferBit);
                }

                obj.BindResources();
                obj.Render(dt);
            }
        }
    }

    public List<RenderableObject> Objects => _objects;
    public List<RenderableObject> ObjectsToAdd => _toAdd;

    private void ProcessPending(GL gl)
    {
        foreach (var obj in _toAdd)
        {
            obj.Init(_shaderStore, _textureStore, _materialStore, gl);

            if (obj is IText textObj)
            {
                textObj.SetMetadata(_fontStore);
            }

            _objects.Add(obj);
        }

        _toAdd.Clear();

        foreach (var obj in _toRemove)
        {
            obj.OnClose();
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
