using System.Numerics;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Bhop;

public class Game : UpdateableObject
{
    private World _world;
    private PlayerObject _player;
    private readonly Tile?[,] _tiles;
    private float _tileSize = 1f;
    
    private int _width;
    private int _depth;

    public Game(World world, PlayerObject player, int[,] map)
    {
        _world = world;
        _player = player;
        
        _width = map.GetLength(0);
        _depth = map.GetLength(1);
        
        _tiles = new Tile[_width, _depth];
        
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _depth; z++)
            {
                int typeInt = map[x, z];
                if (typeInt > 0)
                {
                    Vector3 position = new Vector3(
                        (x * _tileSize), 
                        -10f, 
                        (z * _tileSize)
                    );
                        
                    Tile tile = new Tile(position, "TileShader");
                    _tiles[x, z] = tile;
                    _world.AddObject(tile);
                }
            }
        }
    }

    public override void OnUpdate(double dt)
    {
        int gridX = (int)MathF.Floor((_player.Position.X) / _tileSize);
        int gridZ = (int)MathF.Floor((_player.Position.Z) / _tileSize);

        bool[] map = new bool[9];
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int checkX = gridX + i;
                int checkZ = gridZ + j;
                bool isWall = false;
                if (checkX >= 0 && checkX < _width && checkZ >= 0 && checkZ < _depth)
                {
                    isWall = _tiles[checkX, checkZ] != null;
                }
                else
                {
                    isWall = true;
                }
                map[(i + 1) + (j + 1) * 3] = isWall;
            }
        }

        Vector3 currentCellCenter = new Vector3(
            (gridX * _tileSize) + (_tileSize / 2f),
            0,
            (gridZ * _tileSize) + (_tileSize / 2f)
        );

        _player.SetTileMap(map, currentCellCenter);
    }
}