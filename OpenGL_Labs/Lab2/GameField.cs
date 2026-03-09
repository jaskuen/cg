using System.Drawing;
using System.Numerics;
using Silk.NET.Maths;
using SilkOpenGL;
using SilkOpenGL.Objects;
using SilkOpenGL.Text;

namespace Lab2;

public class GameField : UpdateableObject
{
    private const int ArraySize = 9;
    private const float TileSize = 0.25f;

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

    private Vector3 _fieldPosition = new(-ArraySize * TileSize / 2, ArraySize * TileSize / 2, 0);

    private readonly Tile[,] _tiles;

    private const string ScoreText = "Score: ";
    private int _score;
    private readonly TextObject _scoreObject;

    private Random _random;

    private GameState _state = GameState.SelectBall;
    private Vector2D<int>? _selectedTilePos;

    private Circle? _movingBall;
    private List<Vector3> _animationFrames;

    private int _ballsCount;

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

        _scoreObject = new TextObject(new Vector3(1.2f, 0.95f, 0), Score(), 0.3f, Color.Black);
        _world.AddObject(_scoreObject);

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
            if (_ballsCount == ArraySize * ArraySize) return;

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

            CheckBalls(x, y);
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

            tile.SetActive(true);
            _selectedTilePos = new Vector2D<int>(x, y);
            _state = GameState.BallSelected;
        }
        else if (_state == GameState.BallSelected)
        {
            Tile selectedTile = SelectedTile;
            if (tile.Circle != null)
            {
                _selectedTilePos = new Vector2D<int>(x, y);
                selectedTile.SetActive(false);
                tile.SetActive(true);

                return;
            }

            List<Vector2D<int>>? path = IsPathAvailable(x, y);

            if (path is null)
            {
                return;
            }

            _movingBall = selectedTile.RemoveCircle();
            _animationFrames = GetAnimationFrames(path);
            selectedTile.SetActive(false);
            _selectedTilePos = new Vector2D<int>(x, y);

            _state = GameState.BallMoving;
        }
    }

    private void AnimationFinished(int x, int y)
    {
        Tile tile = GetTile(x, y);

        tile.PlaceCircle(_movingBall!);

        SelectedTile.SetActive(false);
        _selectedTilePos = null;

        if (!CheckBalls(x, y))
        {
            GenerateBalls();
        }

        _state = GameState.SelectBall;
    }

    private List<Vector2D<int>>? IsPathAvailable(int targetX, int targetY)
    {
        Vector2D<int> start = _selectedTilePos!.Value;
        Vector2D<int> target = new Vector2D<int>(targetX, targetY);

        Queue<Vector2D<int>> queue = new Queue<Vector2D<int>>();
        queue.Enqueue(start);

        Dictionary<Vector2D<int>, Vector2D<int>?> parentMap = new();
        parentMap[start] = null;

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

                if (next.X >= 0 && next.X < ArraySize && next.Y >= 0 && next.Y < ArraySize)
                {
                    if (GetTile(next.X, next.Y).Circle == null && !parentMap.ContainsKey(next))
                    {
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        return null;
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

        path.Reverse();
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

    private Tile SelectedTile => GetTile(_selectedTilePos!.Value);

    private Circle CreateCircle(int x, int y)
    {
        Vector3 position = _fieldPosition + new Vector3(x * TileSize, -y * TileSize, 0);

        int color = _random.Next(_colors.Count);
        return new Circle(position, TileSize * 0.4f, _colors[color], Program.TexturedCircleShaderName,
            Program.BallTextureName);
    }

    private bool CheckBalls(int x, int y)
    {
        Tile startTile = GetTile(x, y);
        if (startTile.Circle == null) return false;

        Color targetColor = startTile.BallColor!.Value;
        HashSet<Vector2D<int>> ballsToDelete = new();

        Vector2D<int>[] directions =
        {
            new(1, 0), new(0, 1)
        };

        foreach (var dir in directions)
        {
            List<Vector2D<int>> line = new() { new(x, y) };

            line.AddRange(GetLineInDirection(x, y, dir.X, dir.Y, targetColor));
            line.AddRange(GetLineInDirection(x, y, -dir.X, -dir.Y, targetColor));

            if (line.Count >= 5)
            {
                foreach (var pos in line) ballsToDelete.Add(pos);
            }
        }

        if (ballsToDelete.Count > 0)
        {
            int count = ballsToDelete.Count;
            foreach (var pos in ballsToDelete)
            {
                DeleteBallAt(pos.X, pos.Y);
            }

            _score += 10 + (count - 5) * 5;
            _scoreObject.Text = Score();
            return true;
        }

        return false;
    }

    private List<Vector2D<int>> GetLineInDirection(int startX, int startY, int dx, int dy, Color color)
    {
        List<Vector2D<int>> line = new();
        int cx = startX + dx;
        int cy = startY + dy;

        while (cx >= 0 && cx < ArraySize && cy >= 0 && cy < ArraySize)
        {
            if (GetTile(cx, cy).BallColor == color)
            {
                line.Add(new Vector2D<int>(cx, cy));
                cx += dx;
                cy += dy;
            }
            else break;
        }

        return line;
    }

    private void DeleteBallAt(int x, int y)
    {
        Tile tile = GetTile(x, y);
        if (tile.Circle != null)
        {
            _world.RemoveObject(tile.Circle);
            tile.RemoveCircle();
            _ballsCount--;
        }
    }

    private string Score() => $"{ScoreText}{_score}";
}