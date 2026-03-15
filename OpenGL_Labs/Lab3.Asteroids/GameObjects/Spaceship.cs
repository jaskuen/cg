using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Helpers;
using SilkOpenGL.Objects;

namespace Lab3.Asteroids.GameObjects;

public class Spaceship : RenderableObject, IKeyboardClickable
{
    private Vector3 _velocity = Vector3.Zero;
    private float _rotation = 0f;
    private const float RotationSpeed = 200f;
    private const float Acceleration = 0.08f;
    private const float Drag = 0.98f;
    private const float Scale = 0.1f;

    // Состояния жизни
    private bool _isDead;
    private bool _isInvincible;
    private DateTime _respawnTime;
    private DateTime _invincibilityEndTime;
    
    private const int RespawnSeconds = 3;
    private const int InvincibilitySeconds = 5;

    // Данные обломков
    private List<Debris> _fragments = new();
    private readonly Random _rnd = new();

    public bool IsInvincible => _isInvincible;
    public bool IsDead => _isDead;
    public IKeyboard Keyboard { get; set; }

    public Spaceship(Vector3 pos) : base(Program.LineShaderName)
    {
        _transform.Position = pos;
    }

    protected override void OnInit()
    {
        InitVertices();

        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
        _transform.Scale = Scale;
        
        _gl.BindVertexArray(0);
    }

    public override void OnUpdate(double dt)
    {
        if (_isDead)
        {
            UpdateFragments((float)dt);
            if (DateTime.Now > _respawnTime) PerformReset();
            return;
        }

        if (_isInvincible && DateTime.Now > _invincibilityEndTime)
        {
            _isInvincible = false;
        }

        HandleInput((float)dt);
        
        _transform.Position += _velocity;
        _velocity *= Drag;

        WrapAround();
    }

    private void HandleInput(float dt)
    {
        if (Keyboard.IsKeyPressed(Key.A)) _rotation += RotationSpeed * dt;
        if (Keyboard.IsKeyPressed(Key.D)) _rotation -= RotationSpeed * dt;

        _transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(_rotation));

        if (Keyboard.IsKeyPressed(Key.W))
        {
            float rad = MathHelper.DegreesToRadians(_rotation + 90);
            Vector3 direction = new Vector3(MathF.Cos(rad), MathF.Sin(rad), 0);
            _velocity += direction * Acceleration * dt;
        }
    }

    public void Reset()
    {
        if (_isDead || _isInvincible) return;
        Explode();
    }

    private void Explode()
    {
        _isDead = true;
        _respawnTime = DateTime.Now.AddSeconds(RespawnSeconds);
        _fragments.Clear();

        // Получаем мировые координаты всех 5 вершин
        Vector3[] worldVerts = GetWorldVertices(3);

        // Создаем обломки как пары вершин (линии)
        for (int i = 0; i < worldVerts.Length; i++)
        {
            Vector3 start = worldVerts[i];
            Vector3 end = worldVerts[(i + 1) % worldVerts.Length];
            
            _fragments.Add(new Debris(start, end, _transform.Position, _rnd));
        }
    }

    private void PerformReset()
    {
        _isDead = false;
        _transform.Position = Vector3.Zero;
        _velocity = Vector3.Zero;
        _rotation = 0;
        _isInvincible = true;
        _invincibilityEndTime = DateTime.Now.AddSeconds(InvincibilitySeconds);
    }

    private void UpdateFragments(float dt)
    {
        foreach (var fragment in _fragments)
        {
            fragment.Position += fragment.Velocity * dt;
            fragment.Rotation += fragment.RotationSpeed * dt;
        }
    }

    public override unsafe void OnRender(double dt)
    {
        _shader.Use();

        if (_isDead)
        {
            RenderFragments();
        }
        else
        {
            _shader.SetUniform("uModel", _transform.ModelMatrix);
            _shader.SetUniform("uColor", _isInvincible ? Color.Red : Color.White);
            _vao.Bind();
            _gl.DrawElements(PrimitiveType.LineLoop, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
            _gl.BindVertexArray(0);
        }
    }

    private void RenderFragments()
    {
        // Для каждого обломка строим свою матрицу трансформации
        foreach (var f in _fragments)
        {
            Matrix4x4 model = Matrix4x4.CreateScale(0.1f) * Matrix4x4.CreateRotationZ(f.Rotation) * Matrix4x4.CreateTranslation(f.Position);
            
            _shader.SetUniform("uModel", model);
            _shader.SetUniform("uColor", Color.White);

            // Рисуем одну линию (используем сырые данные вершин обломка)
            // Чтобы не плодить VAO для каждого осколка, используем DrawArrays с временным буфером 
            // или просто рисуем текущий меш корабля с другой логикой (здесь для простоты рисуем через DrawArrays)
            float[] line = { f.LocalStart.X, f.LocalStart.Y, 0, f.LocalEnd.X, f.LocalEnd.Y, 0 };
            
            // В продакшене лучше иметь один VBO для линии и менять его позицию
            _vao.Bind(); 
            _gl.DrawArrays(PrimitiveType.Lines, 0, 2); 
        }
        _gl.BindVertexArray(0);
    }

    private void WrapAround()
    {
        Vector3 pos = _transform.Position;
        if (pos.X > 3.2f) pos.X = -3.2f; else if (pos.X < -3.2f) pos.X = 3.2f;
        if (pos.Y > 2f) pos.Y = -2f; else if (pos.Y < -2f) pos.Y = 2f;
        _transform.Position = pos;
    }

    public (Vector3 position, Vector3 direction, float rotation) GetShootingPoint()
    {
        float rad = MathHelper.DegreesToRadians(_rotation + 90);
        Vector3 dir = new Vector3(MathF.Cos(rad), MathF.Sin(rad), 0);
        return (_transform.Position + dir * 0.05f, dir, _rotation);
    }

    private void InitVertices()
    {
        _indices = [0, 1, 2, 3, 4];
        _vertices = [
            -0.5f, -1f, 0f,
            0f, 1f, 0f,
            0.5f, -1f, 0f,
            0.25f, -0.5f, 0f,
            -0.25f, -0.5f, 0f,
        ];
    }

    // Вспомогательный класс для обломка
    private class Debris
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Rotation;
        public float RotationSpeed;
        public Vector2 LocalStart;
        public Vector2 LocalEnd;

        public Debris(Vector3 start, Vector3 end, Vector3 center, Random rnd)
        {
            Position = (start + end) / 2f; // Центр линии
            LocalStart = new Vector2(start.X - Position.X, start.Y - Position.Y);
            LocalEnd = new Vector2(end.X - Position.X, end.Y - Position.Y);
            
            Vector3 dir = Vector3.Normalize(Position - center);
            Velocity = dir * (float)(rnd.NextDouble() * 0.5f + 0.2f);
            RotationSpeed = (float)(rnd.NextDouble() * 10 - 5);
        }
    }
}