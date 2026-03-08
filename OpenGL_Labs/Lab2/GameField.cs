using System.Drawing;
using System.Numerics;
using Silk.NET.Maths;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab2;

public class GameField : UpdateableObject
{
    private const int ArraySize = 9;
    private const float TileSize = 0.3f;

    private readonly List<Color> _colors =
    [
        Color.DarkGreen,
        Color.Crimson,
        Color.HotPink,
        Color.Purple,
        Color.Yellow,
        Color.Blue,
    ];

    private World _world;

    private Vector3 _fieldPosition = new Vector3(-ArraySize * TileSize / 2, ArraySize * TileSize / 2, 0);

    private readonly Tile[,] _tiles;
    private Random _random;

    private GameState _state = GameState.SelectBall;
    private Vector2D<int>? _selectedTilePos;

    private Circle? _movingBall;
    private List<Vector3> _animationFrames;

    private int _ballsCount = 0;

    public GameField(World world)
    {
        _random = new Random();

        _world = world;
        _tiles = new Tile[ArraySize, ArraySize];
        _animationFrames = [];

        for (int i = 0; i < ArraySize; i++)
        {
            for (int j = 0; j < ArraySize; j++)
            {
                Tile tile = new Tile(_fieldPosition + new Vector3(i * TileSize, -j * TileSize, 0), TileSize,
                    Color.DarkGreen,
                    Program.TileShaderName, Program.TileTextureName);

                _tiles[i, j] = tile;
                int i1 = i;
                int j1 = j;
                _tiles[i, j].HandleClick += () => OnTileClick(i1, j1);
                _world.AddObject(tile);
            }
        }

        GenerateBalls();
    }

    public override void OnUpdate(double dt)
    {
        if (_state == GameState.BallMoving)
        {
            if (_animationFrames.Count > 0)
            {
                _movingBall!.UpdateCenterPosition(_animationFrames[0]);
                _animationFrames.RemoveAt(0);
                return;
            }

            _state = GameState.BallMoved;
        }

        if (_state == GameState.BallMoved)
        {
            AnimationFinished(_selectedTilePos!.Value.X, _selectedTilePos!.Value.Y);
        }
    }

    private void GenerateBalls()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_ballsCount == ArraySize * ArraySize)
            {
                return;
            }

            Tile tile = null;
            int x = 0, y = 0;

            while (tile is null || tile.Circle != null)
            {
                int position = _random.Next(ArraySize * ArraySize);

                x = position / ArraySize;
                y = position % ArraySize;
                tile = _tiles[x, y];
            }

            Circle circle = CreateCircle(x, y);

            tile.PlaceCircle(circle);
            _world.AddObject(circle);

            _ballsCount++;
        }
    }

    private void OnTileClick(int x, int y)
    {
        Tile tile = GetTile(x, y);

        if (_state == GameState.SelectBall)
        {
            if (tile.Circle == null)
            {
                return;
            }

            _selectedTilePos = new Vector2D<int>(x, y);
            _state = GameState.BallSelected;
        }
        else if (_state == GameState.BallSelected)
        {
            if (tile.Circle != null)
            {
                return;
            }

            List<Vector2D<int>>? path = IsPathAvailable(x, y);

            if (path is null)
            {
                return;
            }

            Tile selectedTile = GetTile(_selectedTilePos!.Value);

            _movingBall = selectedTile.RemoveCircle();
            _animationFrames = GetAnimationFrames(path);

            _selectedTilePos = new Vector2D<int>(x, y);

            _state = GameState.BallMoving;
        }
        else if (_state == GameState.BallMoving)
        {
        }
        else if (_state == GameState.BallMoved)
        {
        }
    }

    private void AnimationFinished(int x, int y)
    {
        Tile tile = GetTile(x, y);

        tile.PlaceCircle(_movingBall!);


        _selectedTilePos = null;

        CheckBalls(x, y);

        _state = GameState.SelectBall;

        GenerateBalls();
    }

    private List<Vector2D<int>>? IsPathAvailable(int targetX, int targetY)
    {
        Vector2D<int> start = _selectedTilePos!.Value;
        Vector2D<int> target = new Vector2D<int>(targetX, targetY);

        // Очередь для обхода
        Queue<Vector2D<int>> queue = new Queue<Vector2D<int>>();
        queue.Enqueue(start);

        // Словарь: Клетка -> Откуда мы в неё пришли
        // Используем его и как список посещенных, и для восстановления пути
        Dictionary<Vector2D<int>, Vector2D<int>?> parentMap = new();
        parentMap[start] = null;

        // Возможные направления движения
        Vector2D<int>[] directions =
        [
            new(0, 1), new(0, -1), new(1, 0), new(-1, 0)
        ];

        while (queue.Count > 0)
        {
            Vector2D<int> current = queue.Dequeue();

            if (current.X == targetX && current.Y == targetY)
            {
                return ReconstructPath(parentMap, target);
            }

            foreach (var dir in directions)
            {
                Vector2D<int> next = new Vector2D<int>(current.X + dir.X, current.Y + dir.Y);

                // Проверка границ
                if (next.X >= 0 && next.X < ArraySize && next.Y >= 0 && next.Y < ArraySize)
                {
                    // Если клетка пустая И мы там еще не были
                    if (GetTile(next.X, next.Y).Circle == null && !parentMap.ContainsKey(next))
                    {
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        return null; // Путь не найден
    }

    private List<Vector2D<int>> ReconstructPath(Dictionary<Vector2D<int>, Vector2D<int>?> parentMap,
        Vector2D<int> target)
    {
        List<Vector2D<int>> path = new List<Vector2D<int>>();
        Vector2D<int>? current = target;

        while (current.HasValue)
        {
            path.Add(current.Value);
            current = parentMap[current.Value];
        }

        path.Reverse(); // Путь шел от цели к старту, переворачиваем
        return path;
    }

    private List<Vector3> GetAnimationFrames(List<Vector2D<int>> tiles)
    {
        const int framesPerTileMove = 20;

        List<Vector3> frames = [];

        for (int i = 0; i < tiles.Count - 1; i++)
        {
            Vector3 tile1 = GetTile(tiles[i]).Position;
            Vector3 tile2 = GetTile(tiles[i + 1]).Position;

            Vector3 delta = tile2 - tile1;

            for (int j = 1; j <= framesPerTileMove; j++)
            {
                frames.Add(tile1 + new Vector3(delta.X / framesPerTileMove * j, delta.Y / framesPerTileMove * j, 0));
            }
        }

        return frames;
    }

    private Tile GetTile(int x, int y)
    {
        return _tiles[x, y];
    }

    private Tile GetTile(Vector2D<int> position)
    {
        return _tiles[position.X, position.Y];
    }

    private Circle CreateCircle(int x, int y)
    {
        Vector3 position = _fieldPosition + new Vector3(x * TileSize, -y * TileSize, 0);

        int color = _random.Next(_colors.Count);
        return new Circle(position, TileSize * 0.4f, _colors[color], Program.TexturedCircleShaderName,
            Program.BallTextureName);
    }

    private void CheckBalls(int x, int y)
    {
        for (int dy = Math.Max(0, y - 4); dy < Math.Min(y + 4, ArraySize); dy++)
        {
            int combo = GetComboBallsCount(x, dy, true);
            if (combo > 0)
            {
                DeleteBalls(x, dy, combo, true);

                return;
            }
        }

        for (int dx = Math.Max(0, x - 4); dx < Math.Min(x + 4, ArraySize); dx++)
        {
            int combo = GetComboBallsCount(dx, y, false);
            if (combo > 0)
            {
                DeleteBalls(dx, y, combo, false);

                return;
            }
        }
    }

    private int GetComboBallsCount(int x, int y, bool isVertical)
    {
        int count = 1;

        Color? color = GetTile(x, y).BallColor;

        if (color == null)
        {
            return 0;
        }

        int i = 1;

        while (true)
        {
            int cx = isVertical ? x : x + i;
            int cy = isVertical ? y + i : y;

            i++;

            if (cx >= ArraySize || cy >= ArraySize)
            {
                return count >= 5 ? count : 0;
            }

            if (GetTile(cx, cy).BallColor == color)
            {
                count++;
                continue;
            }

            return count >= 5 ? count : 0;
        }
    }

    private void DeleteBalls(int x, int y, int count, bool isVertical)
    {
        for (int i = 0; i < count; i++)
        {
            int cx = isVertical ? x : x + i;
            int cy = isVertical ? y + i : y;

            Tile tile = GetTile(cx, cy);

            Circle? circle = tile.Circle;

            if (circle != null)
            {
                _world.RemoveObject(circle);

                tile.RemoveCircle();
                _ballsCount--;
            }
        }
    }
}