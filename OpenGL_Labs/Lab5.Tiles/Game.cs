using System.Numerics;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab5.Tiles;

public class Game : UpdateableObject
{
    private readonly World _world;
    private readonly Random _random = new();
    private readonly List<string> _textureKeys;
    private readonly string _shaderKey;

    private readonly List<Tile> _tiles = [];
    private readonly List<Tile> _selectedTiles = [];
    private float _tileSize = 1f;
    private float _tileGap = 0.2f;
    private float _tileDepth = 0.12f;
    private float _boardY = -2.5f;

    private int _selectedTilesAnimationDuration = 1;

    private GameMode _gameMode = GameMode.Easy;
    private int _rows;
    private int _columns;

    private GameState _state;
    private DateTime _animationEndTime;

    public Game(World world, List<string> textureKeys, string shaderKey, GameMode gameMode = GameMode.Easy)
    {
        _world = world;
        _textureKeys = textureKeys;
        _shaderKey = shaderKey;
        _gameMode = gameMode;

        ConfigureMode();
        GenerateTiles();

        _state = GameState.NoTilesSelected;
    }

    public override void OnUpdate(double dt)
    {
        if (_state == GameState.TwoTilesSelected)
        {
            if (_selectedTiles.All(tile => tile.AnimationFinished))
            {
                if (_selectedTiles[0].TextureId == _selectedTiles[1].TextureId)
                {
                    _state = GameState.DeleteTilesAnimation;
                }
                else
                {
                    _state = GameState.WaitTilesAction;
                }

                Console.WriteLine(_state.ToString());

                _animationEndTime = DateTime.Now.AddSeconds(_selectedTilesAnimationDuration);
                return;
            }
        }

        if (_state == GameState.DeleteTilesAnimation)
        {
            if (_animationEndTime < DateTime.Now)
            {
                _state = GameState.NoTilesSelected;
                _selectedTiles.ForEach(t => _world.RemoveObject(t));
                _tiles.Remove(_selectedTiles[0]);
                _tiles.Remove(_selectedTiles[1]);
                _selectedTiles.Clear();
                return;
            }

            float scale = (_animationEndTime - DateTime.Now).Milliseconds / (1000f * _selectedTilesAnimationDuration);

            _selectedTiles.ForEach(tile => tile.SetScale(_tileSize * scale));
        }

        if (_state == GameState.WaitTilesAction)
        {
            if (_animationEndTime < DateTime.Now)
            {
                _state = GameState.TilesAction;
            }

            return;
        }

        if (_state == GameState.TilesAction)
        {
            _selectedTiles.ForEach(tile =>
            {
                tile.Unlock();
                tile.Hide();
            });

            _selectedTiles.Clear();
            _state = GameState.NoTilesSelected;
        }
    }

    private void ConfigureMode()
    {
        switch (_gameMode)
        {
            case GameMode.Easy:
                _rows = 2;
                _columns = 3;
                _tileSize = 1.2f;
                break;
            case GameMode.Normal:
                _rows = 4;
                _columns = 5;
                _tileSize = 1.0f;
                _boardY = -5f;
                break;
            case GameMode.Hard:
                _rows = 5;
                _columns = 8;
                _tileSize = 0.8f;
                _boardY = -5f;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void GenerateTiles()
    {
        int tileCount = _rows * _columns;
        if (tileCount % 2 != 0)
        {
            throw new InvalidOperationException("Tile count must be even for pair matching.");
        }

        int pairsCount = tileCount / 2;
        if (_textureKeys.Count < pairsCount)
        {
            throw new InvalidOperationException(
                $"Not enough unique images for this mode. Need {pairsCount}, but got {_textureKeys.Count}.");
        }

        List<string> chosenTextures = _textureKeys
            .OrderBy(_ => _random.Next())
            .Take(pairsCount)
            .ToList();

        List<string> deck = [];
        foreach (string textureKey in chosenTextures)
        {
            deck.Add(textureKey);
            deck.Add(textureKey);
        }

        deck = deck.OrderBy(_ => _random.Next()).ToList();

        float boardWidth = _columns * _tileSize + (_columns - 1) * _tileGap;
        float boardHeight = _rows * _tileSize + (_rows - 1) * _tileGap;
        float startX = -boardWidth / 2f + _tileSize / 2f;
        float startZ = boardHeight / 2f - _tileSize / 2f;

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _columns; col++)
            {
                int deckIndex = row * _columns + col;
                string textureKey = deck[deckIndex];

                Vector3 position = new(
                    startX + col * (_tileSize + _tileGap),
                    _boardY,
                    startZ - row * (_tileSize + _tileGap)
                );

                Tile tile = new(
                    _shaderKey,
                    textureKey,
                    position,
                    width: _tileSize,
                    height: _tileSize,
                    depth: _tileDepth,
                    isFaceUp: false
                );

                _tiles.Add(tile);
                tile.Clicked += () => OnTileClick(tile);
                _world.AddObject(tile);
            }
        }

        _world.SetCameraViewPoint(new Vector3(0f, _boardY, 0f));
    }

    private void OnTileClick(Tile tile)
    {
        if (_state is GameState.NoTilesSelected or GameState.OneTileSelected)
        {
            if (!tile.IsFaceUp)
            {
                tile.Unlock();
                _selectedTiles.Add(tile);
            }

            _state = _state == GameState.NoTilesSelected ? GameState.OneTileSelected : GameState.TwoTilesSelected;
            return;
        }

        if (_state == GameState.TwoTilesSelected)
        {
            if (!tile.IsFaceUp)
            {
                tile.Lock();
            }
        }
        else
        {
            tile.Lock();
        }
    }
}