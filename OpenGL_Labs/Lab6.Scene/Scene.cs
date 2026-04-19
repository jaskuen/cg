using System.Numerics;
using SilkOpenGL;
using SilkOpenGL.Model;
using SilkOpenGL.Objects;

namespace Lab6.Scene;

public class Scene : UpdateableObject
{
    private readonly World _world;
    private DateTime _updateTime = DateTime.Now.AddSeconds(2);
    private bool _added = true;

    private RenderableObject _field;

    public Scene(World world, RenderableObject field)
    {
        _world = world;

        _field = field;
    }

    public override void OnUpdate(double dt)
    {
        if (DateTime.Now > _updateTime)
        {
            _updateTime = DateTime.Now.AddSeconds(2);
            if (!_added)
            {
                _world.AddObject(_field);
            }
            else
            {
                _world.RemoveObject(_field);
            }

            _added = !_added;
        }
    }
}