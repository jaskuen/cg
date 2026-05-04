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

    public LabyrinthField(World world, int[,] map, Dictionary<WallType, string> materialMap, string shaderKey)
    {
        _world = world;
        _materialMap = materialMap;
        _shaderKey = shaderKey;
        
        int width = map.GetLength(0);
        int depth = map.GetLength(1);
        _walls = new Wall[width, depth];
        
        // Build the labyrinth from the map array
        // We center the labyrinth around (0, 0, 0) based on its size
        float offsetX = (width * _wallSize) / 2.0f;
        float offsetZ = (depth * _wallSize) / 2.0f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int typeInt = map[x, z];
                if (typeInt > 0) // 0 is empty space/path
                {
                    WallType type = (WallType)typeInt;
                    if (_materialMap.TryGetValue(type, out var materialKey))
                    {
                        Vector3 position = new Vector3(
                            (x * _wallSize) - offsetX + (_wallSize / 2f), 
                            0, 
                            (z * _wallSize) - offsetZ + (_wallSize / 2f)
                        );
                        
                        var wall = new Wall(position, type, _shaderKey, materialKey);
                        _walls[x, z] = wall;
                        _world.AddObject(wall);
                    }
                }
            }
        }
    }

    public override void OnUpdate(double dt)
    {
        // Add any dynamic labyrinth logic here if necessary (e.g. moving walls, traps)
    }

    public Wall? GetWallAt(int x, int z)
    {
        if (x >= 0 && x < _walls.GetLength(0) && z >= 0 && z < _walls.GetLength(1))
        {
            return _walls[x, z];
        }
        return null;
    }
}