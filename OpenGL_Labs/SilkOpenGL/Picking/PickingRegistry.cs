using SilkOpenGL.Objects;

namespace SilkOpenGL;

public class PickingRegistry
{
    private uint _nextId = 1; // 0 зарезервирован для "пустоты"
    private readonly Dictionary<uint, IClickable> _objects = new();

    public uint Register(IClickable obj)
    {
        // Если объект уже имеет ID (например, переиспользуется), просто возвращаем его
        if (obj.ColorId != 0) return obj.ColorId;

        uint id = _nextId++;
        obj.ColorId = id;
        _objects[id] = obj;
        return id;
    }

    public void Unregister(IClickable obj)
    {
        if (_objects.ContainsKey(obj.ColorId))
        {
            _objects.Remove(obj.ColorId);
            // Важно: мы не уменьшаем _nextId, чтобы избежать коллизий
        }
    }

    public IClickable? GetObject(uint id) => _objects.GetValueOrDefault(id);
}