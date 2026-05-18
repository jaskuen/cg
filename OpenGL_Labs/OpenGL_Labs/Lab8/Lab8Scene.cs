using Silk.NET.Input;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab8;

public class Lab8Scene : UpdateableObject, IKeyboardClickable
{
    private readonly World _world;
    private bool _isClicked;

    public Lab8Scene(World world)
    {
        _world = world;
    }

    public override void OnUpdate(double dt)
    {
        if (Keyboard.IsKeyPressed(Key.Space))
        {
            if (!_isClicked)
            {
                _isClicked = true;

                switch (_world.RenderMode)
                {
                    case RenderMode.Rasterization:
                        _world.RenderMode = RenderMode.RayTracing;
                        break;
                    case RenderMode.RayTracing:
                        _world.RenderMode = RenderMode.Rasterization;
                        break;
                }
            }

            return;
        }

        _isClicked = false;
    }

    public IKeyboard Keyboard { get; set; }
}