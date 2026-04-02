using System.Numerics;
using Silk.NET.Input;
using SilkOpenGL.Camera;

namespace Lab4.Labyrinth;

public class PlayerObject : CameraObject
{
    private float _radius = 0.2f;
    private float _wallSize = 1f;

    private bool[] _wallMap = [];
    private Vector3 _currentCellCenter;

    public void SetWallMap(bool[] wallMap, Vector3 currentCellCenter)
    {
        _wallMap = wallMap;
        _currentCellCenter = currentCellCenter;
    }

    public override void ProcessKeyboard(IKeyboard keyboard, double dt)
    {
        if (_wallMap.Length != 9)
        {
            return;
        }

        Vector3 frontFixedHeight = Vector3.Normalize(Front with { Y = 0 });
        float speed = 2.5f * (float)dt;

        Vector3 moveDelta = Vector3.Zero;
        if (keyboard.IsKeyPressed(Key.W)) moveDelta += frontFixedHeight * speed;
        if (keyboard.IsKeyPressed(Key.S)) moveDelta -= frontFixedHeight * speed;
        if (keyboard.IsKeyPressed(Key.A)) moveDelta -= Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;
        if (keyboard.IsKeyPressed(Key.D)) moveDelta += Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;

        Position += moveDelta;

        if (keyboard.IsKeyPressed(Key.Q)) Rotate(new Vector2(-100 * speed, 0));
        if (keyboard.IsKeyPressed(Key.E)) Rotate(new Vector2(100 * speed, 0));

        Position = ResolveCollisions(Position);
    }

    private Vector3 ResolveCollisions(Vector3 pos)
    {
        Vector3 resolved = pos;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int index = (i + 1) + (j + 1) * 3;
                if (_wallMap[index])
                {
                    Vector3 wallCenter = _currentCellCenter + new Vector3(i * _wallSize, 0, j * _wallSize);
                    
                    float halfSize = _wallSize / 2f;
                    float wallMinX = wallCenter.X - halfSize;
                    float wallMaxX = wallCenter.X + halfSize;
                    float wallMinZ = wallCenter.Z - halfSize;
                    float wallMaxZ = wallCenter.Z + halfSize;

                    float closestX = Math.Clamp(resolved.X, wallMinX, wallMaxX);
                    float closestZ = Math.Clamp(resolved.Z, wallMinZ, wallMaxZ);

                    float dx = resolved.X - closestX;
                    float dz = resolved.Z - closestZ;
                    float distanceSquared = dx * dx + dz * dz;

                    if (distanceSquared < _radius * _radius)
                    {
                        float distance = MathF.Sqrt(distanceSquared);
                        
                        if (distance > 0.0001f)
                        {
                            float overlap = _radius - distance;
                            resolved.X += (dx / distance) * overlap;
                            resolved.Z += (dz / distance) * overlap;
                        }
                        else
                        {
                            float distToMinX = Math.Abs(resolved.X - wallMinX);
                            float distToMaxX = Math.Abs(resolved.X - wallMaxX);
                            float distToMinZ = Math.Abs(resolved.Z - wallMinZ);
                            float distToMaxZ = Math.Abs(resolved.Z - wallMaxZ);

                            float minDist = Math.Min(Math.Min(distToMinX, distToMaxX), Math.Min(distToMinZ, distToMaxZ));

                            if (minDist == distToMinX) resolved.X = wallMinX - _radius;
                            else if (minDist == distToMaxX) resolved.X = wallMaxX + _radius;
                            else if (minDist == distToMinZ) resolved.Z = wallMinZ - _radius;
                            else resolved.Z = wallMaxZ + _radius;
                        }
                    }
                }
            }
        }

        return resolved;
    }
}