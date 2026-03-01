using System.Numerics;
using Silk.NET.Input;

namespace SilkOpenGL;

public class Camera
{
    private Vector2 _mousePosition = new (0, 0);
    
    public Vector3 Position { get; set; } = new(0, 0, 3);
    public Vector3 Front { get; private set; } = new(0, 0, -1);
    public Vector3 Up { get; private set; } = Vector3.UnitY;

    private float _yaw = -90f;
    private float _pitch = 0f;
    private float _fov = 60f;

    private float _mouseSensitivity = 0.12f;

    public Matrix4x4 ViewMatrix => Matrix4x4.CreateLookAt(Position, Position + Front, Up);

    public Matrix4x4 ProjectionMatrix(float aspectRatio)
        => Matrix4x4.CreatePerspectiveFieldOfView(DegreesToRadians(_fov), aspectRatio, 0.1f, 100f);

    // Опционально: управление камерой (добавь в World.OnUpdate)
    public void ProcessKeyboard(IKeyboard keyboard, float dt)
    {
        float speed = 2.5f * (float)dt;

        if (keyboard.IsKeyPressed(Key.W)) Position += Front * speed;
        if (keyboard.IsKeyPressed(Key.S)) Position -= Front * speed;
        if (keyboard.IsKeyPressed(Key.A)) Position -= Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;
        if (keyboard.IsKeyPressed(Key.D)) Position += Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;
    }

    public void ProcessMouseMove(IMouse mouse, Vector2 newPos)
    {
        Vector2 delta = new Vector2(newPos.X - _mousePosition.X, newPos.Y - _mousePosition.Y);
        _mousePosition = newPos;
        
        Console.WriteLine($"{delta.X} {delta.Y}");
        
        _yaw += delta.X * _mouseSensitivity;
        _pitch -= delta.Y * _mouseSensitivity;

        //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
        _pitch = Math.Clamp(_pitch, -89f, 89f);

        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Cos(DegreesToRadians(_yaw)) * MathF.Cos(DegreesToRadians(_pitch));
        cameraDirection.Y = MathF.Sin(DegreesToRadians(_pitch));
        cameraDirection.Z = MathF.Sin(DegreesToRadians(_yaw)) * MathF.Cos(DegreesToRadians(_pitch));

        Front = Vector3.Normalize(cameraDirection);
    }

    private float DegreesToRadians(float degrees)
    {
        return degrees * MathF.PI / 180.0f;
    }
}