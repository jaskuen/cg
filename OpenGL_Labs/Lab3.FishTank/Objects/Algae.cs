using System.Drawing;
using System.Numerics;
using Lab3.FishTank.Shapes;

namespace Lab3.FishTank.Objects;

public class Algae : Triangle
{
    private float _swaySpeed;
    private float _swayAmount;
    private float _offset;

    public Algae(Random rnd)
    {
        _swaySpeed = 1.0f + (float)rnd.NextDouble();
        _swayAmount = 0.05f + (float)rnd.NextDouble() * 0.1f;
        _offset = (float)rnd.NextDouble() * MathF.PI * 2;
        
        FillColor = Color.DarkGreen;
        OutlineColor = Color.Green;
        HasOutline = true;
    }

    public override void OnUpdate(double dt)
    {
        float sway = MathF.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds * _swaySpeed + _offset) * _swayAmount;
        _transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, sway);
    }
}