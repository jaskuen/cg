using System.Drawing;
using System.Numerics;
using Lab3.Asteroids.GameObjects;
using Silk.NET.Input;
using SilkOpenGL;
using SilkOpenGL.Objects;
using SilkOpenGL.Text;

namespace Lab3.Asteroids;

public class AsteroidsGame : UpdateableObject, IKeyboardClickable
{
    private readonly World _world;
    private readonly List<Bullet> _bullets = new();
    private readonly List<Asteroid> _asteroids = new();
    private Spaceship _player;

    private int _score;
    private int _lives = 3;

    private TextObject _gameText;

    private Random _random;

    private bool _canShoot = true;
    private bool _isFiring = false;

    public IKeyboard Keyboard { get; set; }

    public AsteroidsGame(World world)
    {
        _world = world;
        _random = new Random();
        CreatePlayer();
        for (int i = 0; i < 4; i++) SpawnAsteroid(3);

        _gameText = new TextObject(new Vector3(-2f, 1f, 0f), GetText(), 0.1f, Color.Black);
        _world.AddObject(_gameText);
    }

    private void CreatePlayer()
    {
        _player = new Spaceship(Vector3.Zero);
        _world.AddObject(_player);
    }

    private void CreateBullet(Vector3 pos, Vector3 dir, float rotation)
    {
        var bullet = new Bullet(pos, dir, rotation);
        _bullets.Add(bullet);
        _world.AddObject(bullet);
    }

    private void SpawnAsteroid(int size, Vector3? position = null)
    {
        Vector3 pos = position ?? GetRandomEdgePosition();
        var asteroid = new Asteroid(pos, size, _random);
        _asteroids.Add(asteroid);
        _world.AddObject(asteroid);
    }

    public override void OnUpdate(double dt)
    {
        if (Keyboard.IsKeyPressed(Key.Space))
        {
            if (_canShoot)
            {
                _isFiring = true;
                _canShoot = false;
            }
        }
        else
        {
            _canShoot = true;
        }

        if (_isFiring)
        {
            var shootData = _player.GetShootingPoint();
            CreateBullet(shootData.position, shootData.direction, shootData.rotation);
            _isFiring = false;
        }

        List<Bullet> bulletsToDelete = [];

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            if (_bullets[i].IsDead)
            {
                Bullet bullet = _bullets[i];
                bulletsToDelete.Add(bullet);
                _bullets.Remove(bullet);
            }
        }

        foreach (var bullet in bulletsToDelete)
        {
            _world.RemoveObject(bullet);
            bullet.Dispose();
        }

        UpdateCollisions();
    }

    private bool ArePolygonsIntersecting(Vector3[] poly1, Vector3[] poly2)
    {
        var polygons = new[] { poly1, poly2 };

        foreach (var polygon in polygons)
        {
            for (int i = 0; i < polygon.Length; i++)
            {
                // Нормаль к текущей грани
                Vector3 p1 = polygon[i];
                Vector3 p2 = polygon[(i + 1) % polygon.Length];
                Vector3 edge = p2 - p1;
                Vector3 axis = new Vector3(-edge.Y, edge.X, 0);

                // Проекции полигонов
                (float min1, float max1) = ProjectPolygon(poly1, axis);
                (float min2, float max2) = ProjectPolygon(poly2, axis);

                // Наличие зазора
                if (max1 < min2 || max2 < min1) return false;
            }
        }

        return true;
    }

    private static (float min, float max) ProjectPolygon(Vector3[] vertices, Vector3 axis)
    {
        float min = Vector3.Dot(vertices[0], axis);
        float max = min;
        for (int i = 1; i < vertices.Length; i++)
        {
            float projection = Vector3.Dot(vertices[i], axis);
            if (projection < min) min = projection;
            if (projection > max) max = projection;
        }

        return (min, max);
    }

    private bool IsPointInPolygon(Vector3 point, Vector3[] polygon)
    {
        // ray casting
        bool result = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            if (polygon[i].Y < point.Y && polygon[j].Y >= point.Y ||
                polygon[j].Y < point.Y && polygon[i].Y >= point.Y)
            {
                if (polygon[i].X + (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) *
                    (polygon[j].X - polygon[i].X) < point.X)
                {
                    result = !result;
                }
            }

            j = i;
        }

        return result;
    }

    private void UpdateCollisions()
    {
        Asteroid? asteroidToDelete = null;

        foreach (var ast in _asteroids)
        {
            Vector3[] asteroidShape = ast.GetWorldVertices(3);

            foreach (var bullet in _bullets)
            {
                if (IsPointInPolygon(bullet.Position, asteroidShape) && !bullet.IsDead)
                {
                    bullet.IsDead = true;
                    asteroidToDelete = ast;
                    break;
                }
            }

            if (!_player.IsInvincible && !_player.IsDead && ArePolygonsIntersecting(_player.GetWorldVertices(3), asteroidShape))
            {
                LoseLive();
                asteroidToDelete = ast;
            }
        }

        if (asteroidToDelete != null)
        {
            DestroyAsteroid(asteroidToDelete);
        }
    }

    private void DestroyAsteroid(Asteroid asteroidToDelete)
    {
        _score += asteroidToDelete.Size * 50;
        _gameText.Text = GetText();
        
        HandleAsteroidHit(asteroidToDelete);
        _asteroids.Remove(asteroidToDelete);
        asteroidToDelete.Dispose();
    }

    private void HandleAsteroidHit(Asteroid ast)
    {
        if (ast.Size > 1)
        {
            SpawnAsteroid(ast.Size - 1, ast.Position);
            SpawnAsteroid(ast.Size - 1, ast.Position);
        }

        _world.RemoveObject(ast);
    }

    private Vector3 GetRandomEdgePosition()
    {
        Vector3 pos = Vector3.Zero;

        float deltaX = 0.5f - (float)_random.NextDouble();
        float deltaY = 0.5f - (float)_random.NextDouble();

        if (deltaX < 0)
        {
            pos = pos with { X = -1.7f + deltaX };
        }
        else
        {
            pos = pos with { X = 1.7f + deltaX };
        }

        if (deltaY < 0)
        {
            pos = pos with { Y = -1.7f + deltaY };
        }
        else
        {
            pos = pos with { Y = 1.7f + deltaY };
        }

        return pos;
    }

    private void LoseLive()
    {
        if (_lives > 1)
        {
            _player.Reset();
            _lives--;
            _gameText.Text = GetText();
            return;
        }

        _world.RemoveObject(_player);
        foreach (Asteroid ast in _asteroids)
        {
            _world.RemoveObject(ast);
        }

        foreach (Bullet bullet in _bullets)
        {
            _world.RemoveObject(bullet);
        }
        
        _world.RemoveObject(_gameText);

        GameOver();
    }

    private void GameOver()
    {
         TextObject gameOver = new TextObject(new Vector3(-2f, 0f, 0f), $"Game over! Score: {_score}", 0.35f, Color.Orange);
         _world.AddObject(gameOver);
    }

    private string GetText() => $"Lives: {_lives}\nScore: {_score}";
}