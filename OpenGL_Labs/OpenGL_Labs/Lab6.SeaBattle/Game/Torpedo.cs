using System.Numerics;
using SilkOpenGL.Model;
using SilkOpenGL.Objects;

namespace Lab6.SeaBattle.Game;

internal sealed class Torpedo
{
    private readonly ModelObject _model;
    private readonly ModelAsset _asset;
    private readonly Vector3 _direction;
    private readonly float _speed;

    public Torpedo(ModelAsset asset, Vector3 position, Vector3 direction, float speed)
    {
        _asset = asset;
        _model = new ModelObject(Program.BasicShader, asset.Data);
        Position = position;
        _direction = Vector3.Normalize(direction);
        _speed = speed;
        ApplyTransform();
    }

    public Vector3 Position { get; private set; }
    public bool ShouldRemove { get; private set; }
    public RenderableObject Renderable => _model;

    public void Update(float dt)
    {
        Position += _direction * _speed * dt;
        ApplyTransform();
    }

    public void MarkRemoved() => ShouldRemove = true;

    private void ApplyTransform()
    {
        _model.Transform.Position = Position;
        _model.Transform.Scale = new Vector3(_asset.Scale);
        _model.Transform.Rotation = TransformHelpers.RotationBetween(_asset.ForwardAxis, _direction);
    }
}
