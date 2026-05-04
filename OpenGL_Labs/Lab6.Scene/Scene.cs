using System.Numerics;
using Silk.NET.Input;
using SilkOpenGL;
using SilkOpenGL.Model;
using SilkOpenGL.Objects;

namespace Lab6.Scene;

public class Scene : UpdateableObject, IKeyboardClickable
{
    private readonly World _world;
    private readonly RenderableObject _ball;
    private readonly RenderableObject _kageyama;
    private bool _restartAnimation = false;

    private float _startBallSpeedY = 0.3f;
    private float _currentBallSpeedY;
    private Vector3 _startPosition = new(-3f, 2f, 18f);
    private Vector3 _ballDirection = Vector3.Normalize(new Vector3(6.5f, 0f, -35f));

    private Vector3 _kageyamaDirection = new(0, 1f, -5f);
    private Vector3 _kageyamaStartPosition = new Vector3(3f, 0.5f, -13f);
    private bool _kageyamaFlying;

    public Scene(World world, RenderableObject ball, RenderableObject kageyama)
    {
        _world = world;
        _ball = ball;
        _kageyama = kageyama;

        Init();
    }

    public override void OnUpdate(double dt)
    {
        if (Keyboard.IsKeyPressed(Key.R))
        {
            _restartAnimation = true;
        }

        if (_restartAnimation)
        {
            Init();
            _restartAnimation = false;
        }

        float ddt = 0.15f;

        Vector3 delta = (_ballDirection with { Y = _currentBallSpeedY }) * ddt;

        _ball.Transform.Position += delta;
        _currentBallSpeedY -= ddt * 0.02f;

        if (_ball.Transform.Position.Y < 0f)
        {
            _currentBallSpeedY *= -1;
            _ball.Transform.Position = _ball.Transform.Position with { Y = 0f };
        }

        if (_ball.Transform.Position.Z < _kageyamaStartPosition.Z && !_kageyamaFlying)
        {
            _kageyamaFlying = true;
        }

        if (_kageyamaFlying)
        {
            _kageyama.Transform.Position += _kageyamaDirection;
            Random random = new();
            _kageyama.Transform.Rotation = new Quaternion((float)random.NextDouble(), (float)random.NextDouble(),
                (float)random.NextDouble(), 1);
        }
    }

    public IKeyboard Keyboard { get; set; }

    private void Init()
    {
        _currentBallSpeedY = _startBallSpeedY;
        _ball.Transform.Position = _startPosition;

        _kageyama.Transform.Position = _kageyamaStartPosition;
        _kageyama.Transform.Rotation = new Quaternion(0, 0, 0, 1);
        _kageyamaFlying = false;
    }
}