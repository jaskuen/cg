using System.Numerics;
using Silk.NET.Input;
using SilkOpenGL.Camera;

namespace Bhop;

public class PlayerObject : CameraObject
{
    private bool _standing = true;
    private float _radius = 0.2f;

    private float _tileSize = 1f;
    private bool[] _tileMap = [];
    private Vector3 _currentCellCenter;

    private float _jumpStrength = 1f;
    private float _g = 0.005f;
    private float _baseSpeed = 2.5f;
    private float _speed = 2.5f;

    private float _zeroY;
    private float _floorY = -1f;

    public float SpeedY { get; set; }

    public PlayerObject()
    {
        Position = Position with { Y = 3f };
        _zeroY = Position.Y;
    }

    public void SetTileMap(bool[] tileMap, Vector3 currentCellCenter)
    {
        _tileMap = tileMap;
        _currentCellCenter = currentCellCenter;
    }

    public override void ProcessKeyboard(IKeyboard keyboard, double dt)
    {
        if (_tileMap.Length != 9)
        {
            return;
        }

        Vector3 frontFixedHeight = Vector3.Normalize(Front with { Y = 0 });
        float speed = _speed * (float)dt;

        Vector3 moveDelta = Vector3.Zero;
        if (keyboard.IsKeyPressed(Key.W) || !_standing) moveDelta += frontFixedHeight * speed;
        if (keyboard.IsKeyPressed(Key.S)) moveDelta -= frontFixedHeight * speed;
        if (keyboard.IsKeyPressed(Key.A)) moveDelta -= Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;
        if (keyboard.IsKeyPressed(Key.D)) moveDelta += Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;
        if (keyboard.IsKeyPressed(Key.Space) && _standing)
        {
            _standing = false;
            Jump();
        }

        Position += moveDelta;

        Position += HeightDelta();
        if (!_standing)
        {
            SpeedY -= _g;
        }

        _standing = IsStanding();
    }

    private void Jump()
    {
        SpeedY = _jumpStrength;
    }

    private Vector3 HeightDelta() => new(0, SpeedY, 0);

    private bool IsStanding()
    {
        bool isStanding = false;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int index = (i + 1) + (j + 1) * 3;
                if (_tileMap[index])
                {
                    Vector3 wallCenter = _currentCellCenter + new Vector3(i * _tileSize, 0, j * _tileSize);

                    float halfSize = _tileSize / 2f;
                    float wallMinX = wallCenter.X - halfSize;
                    float wallMaxX = wallCenter.X + halfSize;
                    float wallMinZ = wallCenter.Z - halfSize;
                    float wallMaxZ = wallCenter.Z + halfSize;

                    if (Position.X > wallMinX && Position.X < wallMaxX)
                    {
                        if (Position.Z > wallMinZ && Position.Z < wallMaxZ)
                        {
                            if (Math.Abs(Position.Y - _floorY) < 0.1f)
                            {
                                isStanding = true;
                            }
                        }
                    }
                }
            }
        }

        return isStanding;
    }
}