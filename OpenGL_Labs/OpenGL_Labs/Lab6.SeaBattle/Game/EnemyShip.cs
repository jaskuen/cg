using System.Numerics;
using SilkOpenGL.Model;
using SilkOpenGL.Objects;

namespace Lab6.SeaBattle.Game;

internal sealed class EnemyShip
{
    private readonly ModelObject _model;
    private readonly ModelAsset _asset;
    private readonly float _direction;
    private readonly float _speed;
    private float _sinkDepth;
    private float _roll;

    private EnemyShip(
        ModelObject model,
        ModelAsset asset,
        ShipVariant variant,
        Vector3 position,
        float direction,
        float speed,
        float hitRadius,
        int scoreValue)
    {
        _model = model;
        _asset = asset;
        Variant = variant;
        Position = position;
        _direction = direction;
        _speed = speed;
        HitRadius = hitRadius;
        ScoreValue = scoreValue;
        ApplyTransform();
    }

    public ShipVariant Variant { get; }
    public Vector3 Position { get; private set; }
    public float HitRadius { get; }
    public int ScoreValue { get; }
    public bool IsSinking { get; private set; }
    public bool ShouldRemove { get; private set; }
    public RenderableObject Renderable => _model;

    public static EnemyShip Create(ShipVariant variant, ModelAsset asset, Vector3 position, float direction, float speed)
    {
        ModelObject model = new(Program.BasicShader, asset.Data);
        float radius = variant switch
        {
            ShipVariant.Destroyer => 1.75f,
            ShipVariant.Cruiser => 2.15f,
            ShipVariant.Carrier => 2.75f,
            _ => 2f
        };
        int score = variant switch
        {
            ShipVariant.Destroyer => 120,
            ShipVariant.Cruiser => 90,
            ShipVariant.Carrier => 70,
            _ => 100
        };

        return new EnemyShip(model, asset, variant, position, direction, speed, radius, score);
    }

    public void Update(float dt)
    {
        if (ShouldRemove)
        {
            return;
        }

        if (IsSinking)
        {
            _sinkDepth += dt * 0.85f;
            _roll += dt * 0.65f;
            Position += new Vector3(_direction * dt * _speed * 0.25f, 0f, 0f);
            if (_sinkDepth > 2.4f)
            {
                ShouldRemove = true;
            }
        }
        else
        {
            Position += new Vector3(_direction * _speed * dt, 0f, 0f);
        }

        ApplyTransform();
    }

    public void Sink()
    {
        IsSinking = true;
    }

    public void MarkRemoved() => ShouldRemove = true;

    private void ApplyTransform()
    {
        Vector3 forward = new(_direction, 0f, 0f);
        Quaternion faceTravelDirection = TransformHelpers.RotationBetween(_asset.ForwardAxis, forward);
        Quaternion sinkingRoll = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, _roll);

        _model.Transform.Position = new Vector3(
            Position.X,
            Position.Y + _asset.WaterlineOffset - _sinkDepth,
            Position.Z);
        _model.Transform.Scale = new Vector3(_asset.Scale);
        _model.Transform.Rotation = sinkingRoll * faceTravelDirection;
    }
}
