using System.Numerics;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab4.Labyrinth;

public class LabyrinthField : UpdateableObject
{
    private readonly World _world;
    private readonly Wall?[,] _walls;
    private readonly float _wallSize = 1.0f;
    private readonly Dictionary<WallType, string> _materialMap;
    private readonly string _shaderKey;
    
    private PlayerObject _player;

    private int _width;
    private int _depth;
    private float _offsetX;
    private float _offsetZ;

    public LabyrinthField(World world, PlayerObject player, int[,] map, Dictionary<WallType, string> materialMap, string shaderKey)
    {
        _world = world;
        _player = player;
        player.SetWallSize(_wallSize);
        _materialMap = materialMap;
        _shaderKey = shaderKey;
        
        _width = map.GetLength(0);
        _depth = map.GetLength(1);
        _walls = new Wall[_width, _depth];
        
        _offsetX = (_width * _wallSize) / 2.0f;
        _offsetZ = (_depth * _wallSize) / 2.0f;

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _depth; z++)
            {
                int typeInt = map[x, z];
                if (typeInt > 0)
                {
                    WallType type = (WallType)typeInt;
                    if (_materialMap.TryGetValue(type, out var materialKey))
                    {
                        Vector3 position = new Vector3(
                            (x * _wallSize) - _offsetX + (_wallSize / 2f), 
                            0, 
                            (z * _wallSize) - _offsetZ + (_wallSize / 2f)
                        );
                        
                        var wall = new Wall(position, type, _shaderKey, materialKey);
                        _walls[x, z] = wall;
                        _world.AddObject(wall);
                    }
                }
            }
        }

        Floor floor = new Floor(_width, "FloorShader");
        _world.AddObject(floor);
    }

    public override void OnUpdate(double dt)
    {
        int gridX = (int)MathF.Floor((_player.Position.X + _offsetX) / _wallSize);
        int gridZ = (int)MathF.Floor((_player.Position.Z + _offsetZ) / _wallSize);

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
                    isWall = _walls[checkX, checkZ] != null;
                }
                else
                {
                    isWall = true;
                }
                map[(i + 1) + (j + 1) * 3] = isWall;
            }
        }

        Vector3 currentCellCenter = new Vector3(
            (gridX * _wallSize) - _offsetX + (_wallSize / 2f),
            0,
            (gridZ * _wallSize) - _offsetZ + (_wallSize / 2f)
        );

        _player.SetWallMap(map, currentCellCenter);
    }
}