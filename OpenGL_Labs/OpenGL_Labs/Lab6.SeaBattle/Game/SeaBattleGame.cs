using System.Drawing;
using System.Numerics;
using Lab6.SeaBattle.Rendering;
using Silk.NET.Input;
using SilkOpenGL;
using SilkOpenGL.Objects;
using SilkOpenGL.Text;

namespace Lab6.SeaBattle.Game;

public sealed class SeaBattleGame : UpdateableObject, IKeyboardClickable
{
    private const float TorpedoCooldownSeconds = 1.5f;
    private const float ShipLimitX = 19f;
    private const float AimTurnSpeed = 1.45f;
    private const float MaxAimYaw = 0.92f;
    private const string Shader = Program.BasicShader;

    private readonly World _world;
    private readonly SeaBattleModelAssets _assets = new();
    private readonly Random _random = new();
    private readonly List<EnemyShip> _ships = [];
    private readonly List<Torpedo> _torpedoes = [];
    private readonly List<PrimitiveObject> _aimObjects = [];

    private TextObject? _hud;
    private TextObject? _gameOver;
    private int _lives = 3;
    private int _score;
    private float _cooldownRemaining;
    private float _spawnTimer = 1f;
    private float _aimYaw;
    private bool _isGameOver;
    private bool _rWasPressed;
    private bool _spaceWasPressed;

    public SeaBattleGame(World world)
    {
        _world = world;
    }

    public IKeyboard Keyboard { get; set; } = null!;

    public void Start()
    {
        PrimitiveObject sea = new(Shader, MeshFactory.WaterPlane(55f, 42f), new Vector3(0.02f, 0.25f, 0.42f), 0.18f,
            0.05f, 0.88f);
        sea.Transform.Position = new Vector3(0f, -0.06f, -14f);
        _world.AddObject(sea);

        CreateAimArrow();

        _hud = new TextObject(new Vector3(-3.7f, 7.05f, -5.4f), HudText(), 0.18f, Color.White);
        _world.AddObject(_hud);
    }

    public override void OnUpdate(double dt)
    {
        float delta = MathF.Min((float)dt, 0.05f);
        HandleRestart();

        if (_isGameOver)
        {
            UpdateSinkingShips(delta);
            UpdateAimArrow();
            UpdateHud();
            return;
        }

        HandleAimInput(delta);
        _cooldownRemaining = MathF.Max(0f, _cooldownRemaining - delta);
        _spawnTimer -= delta;
        if (_spawnTimer <= 0f)
        {
            SpawnShip();
            _spawnTimer = RandomRange(1.35f, 2.4f);
        }

        if (!_isGameOver)
        {
            UpdateShips(delta);
            UpdateTorpedoes(delta);
            ResolveCollisions();
            CleanupRemoved();
            UpdateAimArrow();
            UpdateHud();
        }
    }

    private void Fire()
    {
        if (_isGameOver || _cooldownRemaining > 0f)
        {
            SoundEffects.Cooldown();
            return;
        }

        Vector3 origin = new(0f, 0.22f, -6.5f);
        Vector3 direction = AimDirection();

        Torpedo torpedo = new(_assets.Torpedo, origin, direction, 14.5f);
        _world.AddObject(torpedo.Renderable);
        _torpedoes.Add(torpedo);
        _cooldownRemaining = TorpedoCooldownSeconds;
        SoundEffects.Shot();
    }

    private void CreateAimArrow()
    {
        PrimitiveObject shaft = new(Shader, MeshFactory.Cylinder(20), new Vector3(0.9f, 0.75f, 0.12f), 0.28f, 0.25f);
        shaft.Transform.Scale = new Vector3(0.055f, 0.055f, 1.85f);
        _aimObjects.Add(shaft);
        _world.AddObject(shaft);

        PrimitiveObject head = new(Shader, MeshFactory.Cone(20), new Vector3(1f, 0.18f, 0.08f), 0.35f, 0.2f);
        head.Transform.Scale = new Vector3(0.18f, 0.18f, 0.48f);
        _aimObjects.Add(head);
        _world.AddObject(head);

        UpdateAimArrow();
    }

    private void UpdateAimArrow()
    {
        Vector3 origin = new(0f, 0.55f, -6.5f);
        Vector3 direction = AimDirection();
        Quaternion rotation = DirectionToRotation(direction);

        _aimObjects[0].Transform.Position = origin + direction * 1.1f;
        _aimObjects[0].Transform.Rotation = rotation;
        _aimObjects[1].Transform.Position = origin + direction * 2.25f;
        _aimObjects[1].Transform.Rotation = rotation;
    }

    private void HandleAimInput(float dt)
    {
        if (Keyboard is null)
        {
            return;
        }

        if (Keyboard.IsKeyPressed(Key.A))
        {
            _aimYaw -= AimTurnSpeed * dt;
        }

        if (Keyboard.IsKeyPressed(Key.D))
        {
            _aimYaw += AimTurnSpeed * dt;
        }

        _aimYaw = Math.Clamp(_aimYaw, -MaxAimYaw, MaxAimYaw);

        bool spacePressed = Keyboard.IsKeyPressed(Key.Space);
        if (spacePressed && !_spaceWasPressed)
        {
            Fire();
        }

        _spaceWasPressed = spacePressed;
    }

    private Vector3 AimDirection()
    {
        return Vector3.Normalize(new Vector3(MathF.Sin(_aimYaw), 0f, -MathF.Cos(_aimYaw)));
    }

    private void SpawnShip()
    {
        ShipVariant variant = (ShipVariant)_random.Next(0, 3);
        float direction = _random.Next(0, 2) == 0 ? 1f : -1f;
        float startX = direction > 0f ? -ShipLimitX : ShipLimitX;
        float z = RandomRange(-30f, -17f);
        float speed = variant switch
        {
            ShipVariant.Destroyer => RandomRange(2.7f, 3.5f),
            ShipVariant.Cruiser => RandomRange(1.7f, 2.35f),
            ShipVariant.Carrier => RandomRange(1.15f, 1.75f),
            _ => 2f
        };

        EnemyShip ship = EnemyShip.Create(variant, _assets.Ship(variant), new Vector3(startX, 0.18f, z), direction,
            speed);
        _world.AddObject(ship.Renderable);
        _ships.Add(ship);
    }

    private void UpdateShips(float dt)
    {
        foreach (EnemyShip ship in _ships)
        {
            ship.Update(dt);

            if (!ship.IsSinking && MathF.Abs(ship.Position.X) > ShipLimitX + 1.8f)
            {
                ship.MarkRemoved();
                _lives--;
                SoundEffects.Miss();
                if (_lives <= 0)
                {
                    EndGame();
                }
            }
        }
    }

    private void UpdateSinkingShips(float dt)
    {
        foreach (EnemyShip ship in _ships)
        {
            ship.Update(dt);
        }

        CleanupRemoved();
    }

    private void UpdateTorpedoes(float dt)
    {
        foreach (Torpedo torpedo in _torpedoes)
        {
            torpedo.Update(dt);
            if (torpedo.Position.Z < -42f || MathF.Abs(torpedo.Position.X) > 35f || torpedo.Position.Y > 5f ||
                torpedo.Position.Y < -2f)
            {
                torpedo.MarkRemoved();
            }
        }
    }

    private void ResolveCollisions()
    {
        foreach (Torpedo torpedo in _torpedoes.Where(t => !t.ShouldRemove))
        {
            foreach (EnemyShip ship in _ships.Where(s => !s.IsSinking && !s.ShouldRemove))
            {
                float distance = Vector3.Distance(torpedo.Position, ship.Position);
                if (distance > ship.HitRadius)
                {
                    continue;
                }

                torpedo.MarkRemoved();
                ship.Sink();
                _score += ship.ScoreValue;
                SoundEffects.Explosion();
                break;
            }
        }
    }

    private void CleanupRemoved()
    {
        for (int i = _torpedoes.Count - 1; i >= 0; i--)
        {
            if (!_torpedoes[i].ShouldRemove)
            {
                continue;
            }

            _world.RemoveObject(_torpedoes[i].Renderable);
            _torpedoes.RemoveAt(i);
        }

        for (int i = _ships.Count - 1; i >= 0; i--)
        {
            if (!_ships[i].ShouldRemove)
            {
                continue;
            }

            _world.RemoveObject(_ships[i].Renderable);
            _ships.RemoveAt(i);
        }
    }

    private void EndGame()
    {
        _isGameOver = true;
        _lives = 0;
        RemoveObjects();
        _gameOver = new TextObject(new Vector3(-3.4f, 2.35f, -6.2f), "GAME OVER  Press R", 0.26f, Color.Orange);
        _world.AddObject(_gameOver);
        SoundEffects.GameOver();
    }

    private void HandleRestart()
    {
        if (Keyboard is null)
        {
            return;
        }

        bool rPressed = Keyboard.IsKeyPressed(Key.R);
        if (rPressed && !_rWasPressed && _isGameOver)
        {
            Reset();
        }

        _rWasPressed = rPressed;
    }

    private void RemoveObjects()
    {
        foreach (EnemyShip ship in _ships)
        {
            _world.RemoveObject(ship.Renderable);
        }

        foreach (Torpedo torpedo in _torpedoes)
        {
            _world.RemoveObject(torpedo.Renderable);
        }

        _ships.Clear();
        _torpedoes.Clear();
    }

    private void Reset()
    {
        RemoveObjects();
        
        if (_gameOver != null)
        {
            _world.RemoveObject(_gameOver);
            _gameOver = null;
        }

        _lives = 3;
        _score = 0;
        _cooldownRemaining = 0f;
        _spawnTimer = 0.5f;
        _aimYaw = 0f;
        _spaceWasPressed = false;
        _isGameOver = false;
    }

    private void UpdateHud()
    {
        if (_hud != null)
        {
            _hud.Text = HudText();
        }
    }

    private string HudText()
    {
        string torpedo = _cooldownRemaining <= 0f ? "READY" : $"{_cooldownRemaining:0.0}s";
        int angle = (int)MathF.Round(_aimYaw * 180f / MathF.PI);
        return $"Lives: {_lives}   Score: {_score}   Torpedo: {torpedo}   Aim: {angle} deg   A/D aim   Space fire";
    }

    private float RandomRange(float min, float max) => min + (float)_random.NextDouble() * (max - min);

    private static Quaternion DirectionToRotation(Vector3 direction)
    {
        Vector3 from = Vector3.UnitZ;
        Vector3 to = Vector3.Normalize(direction);
        float dot = Vector3.Dot(from, to);
        if (dot > 0.9999f)
        {
            return Quaternion.Identity;
        }

        if (dot < -0.9999f)
        {
            return Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
        }

        Vector3 axis = Vector3.Normalize(Vector3.Cross(from, to));
        float angle = MathF.Acos(Math.Clamp(dot, -1f, 1f));
        return Quaternion.CreateFromAxisAngle(axis, angle);
    }
}