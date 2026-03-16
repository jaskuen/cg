using System.Drawing;
using System.Numerics;
using Lab3.FishTank.Shapes;
using SilkOpenGL;
using SilkOpenGL.Objects;
using Rectangle = Lab3.FishTank.Shapes.Rectangle;

namespace Lab3.FishTank.Objects;

public class Fish : UpdateableObject, IDisposable
{
    private readonly Shape _body;
    private readonly Triangle _tail;
    private readonly Triangle _fin;
    private readonly Ellipse _eye;

    private readonly Color _bodyColor;
    private readonly int _bodyType;

    private readonly Transform _transform;
    private float _velocity;

    public List<RenderableObject> Figures => [_body, _tail, _fin, _eye];

    public Fish(Vector3 position, Color bodyColor, int bodyType, float velocity)
    {
        Vector3 scale = velocity > 0
            ? new Vector3(0.2f)
            : new Vector3(-0.2f, 0.2f, 0.2f);

        _transform = new Transform
        {
            Position = position,
            Scale = scale
        };

        _bodyColor = bodyColor;
        _bodyType = bodyType;
        _velocity = velocity;

        _body = _bodyType switch
        {
            0 => new Ellipse(),
            1 => new Rectangle(),
            2 => new Triangle(),
        };
        _tail = new Triangle();
        _fin = new Triangle();
        _eye = new Ellipse(16);

        ConfigureAppearance();
    }

    private void ConfigureAppearance()
    {
        // Тело
        _body.FillColor = _bodyColor;
        _body.OutlineColor = _bodyColor;
        _body.HasOutline = true;
        _body.OutlineWidth = 2.0f;
        if (_bodyType == 0)
        {
            _body.SetScale(new Vector3(1.0f, 0.6f, 1.0f) * _transform.Scale); // Вытянутый эллипс
        }
        else if (_bodyType == 1)
        {
            _body.SetScale(new Vector3(1.5f, 0.7f, 1.0f) * _transform.Scale);
        }
        else
        {
            _body.SetScale(new Vector3(1.0f, 2.5f, 1.0f) * _transform.Scale);
            _body.SetRotation((_velocity > 0 ? 1 : -1) * MathF.PI / 2f);
        }
        

        // Хвост
        _tail.FillColor = Color.Red;
        _tail.HasOutline = false;
        _tail.SetScale(new Vector3(-0.8f, 0.8f, 0.8f) * _transform.Scale);
        _tail.SetRotation((_velocity > 0 ? 1 : -1) * -MathF.PI / 2f);

        // Плавник
        _fin.FillColor = Color.Yellow;
        _fin.HasOutline = true;
        _fin.OutlineColor = Color.Orange;
        _fin.SetScale(new Vector3(0.3f, 0.4f, 1.0f) * _transform.Scale);

        // Глаз (белый с черным зрачком - зрачок можно добавить вторым эллипсом)
        _eye.FillColor = Color.White;
        _eye.OutlineColor = Color.White;
        _eye.HasOutline = true;
        _eye.SetScale(new Vector3(0.1f) * _transform.Scale);
    }

    public override void OnUpdate(double dt)
    {
        // "Плавание" (легкое покачивание)
        float wave = MathF.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds * 2.0f);
        _transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, wave * 0.1f);

        // Обновляем позиции компонентов ОТНОСИТЕЛЬНО центра рыбы
        UpdateComponentsTransform();

        // Вызываем OnUpdate для компонентов (если у них есть анимация)
        _body.OnUpdate(dt);
        _tail.OnUpdate(dt);
        _fin.OnUpdate(dt);
        _eye.OnUpdate(dt);

        // Простая логика WrapAround для экрана
        WrapAround();
    }

    private void UpdateComponentsTransform()
    {
        _transform.Position += new Vector3(_velocity, 0, 0);

        // Матрица модели всей рыбы
        Matrix4x4 fishModel = _transform.ModelMatrix;

        // Позиционируем компоненты в локальном пространстве рыбы
        // И умножаем на матрицу рыбы, чтобы они двигались вместе

        // Тело в центре
        _body.SetPosition(Vector3.Transform(Vector3.Zero, fishModel));

        // Хвост сзади (-X)
        _tail.SetPosition(Vector3.Transform(new Vector3(-0.9f, 0, 0), fishModel));

        // Плавник сверху (+Y)
        _fin.SetPosition(Vector3.Transform(new Vector3(0, 0.5f, 0), fishModel));

        // Глаз спереди (+X) и сверху (+Y)
        _eye.SetPosition(Vector3.Transform(new Vector3(0.6f, 0.2f, 0), fishModel));
    }

    private void WrapAround()
    {
        Vector3 pos = _transform.Position;
        if (pos.X > 3.5f) pos.X = -3.5f;
        else if (pos.X < -3.5f) pos.X = 3.5f;
        if (pos.Y > 2.5f) pos.Y = -2.5f;
        else if (pos.Y < -2.5f) pos.Y = 2.5f;
        _transform.Position = pos;
    }

    public void Dispose()
    {
        // Освобождаем ресурсы GPU компонентов
        _body.Dispose();
        _tail.Dispose();
        _fin.Dispose();
        _eye.Dispose();
    }
}