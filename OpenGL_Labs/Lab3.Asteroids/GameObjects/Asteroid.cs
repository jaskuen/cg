using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Helpers;
using SilkOpenGL.Objects;

namespace Lab3.Asteroids.GameObjects;

public class Asteroid : RenderableObject
{
    private const int SegmentsCount = 12;
    private const float Roughness = 0.2f;

    private Random _random;
    public int Size { get; }
    public float Radius => Size * 0.1f;
    public bool IsPendingDeletion { get; set; }

    private Vector3 _velocity;
    private float _spinSpeed;

    public Asteroid(Vector3 pos, int size, Random random) : base(Program.LineShaderName)
    {
        Size = size;
        _transform.Scale = Radius;
        _transform.Position = pos;

        _random = random;
        float angle = (float)_random.NextDouble() * MathF.PI * 2;
        _velocity = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0) * 0.002f;
        _spinSpeed = (float)(_random.NextDouble() - 0.5) * 100f;
    }

    protected override void OnInit()
    {
        GenerateAsteroidMesh();

        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
    }

    public override void OnUpdate(double dt)
    {
        _transform.Position += _velocity;

        // Вращаем астероид вокруг оси Z для красоты
        float currentRot = _transform.Rotation.Z + _spinSpeed * (float)dt;
        _transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(currentRot));

        WrapAround();
    }

    private void WrapAround()
    {
        if (_transform.Position.X > 3.2f) _transform.Position = _transform.Position with { X = -3.2f };
        else if (_transform.Position.X < -3.2f) _transform.Position = _transform.Position with { X = 3.2f };

        if (_transform.Position.Y > 2f) _transform.Position = _transform.Position with { Y = -2f };
        else if (_transform.Position.Y < -2f) _transform.Position = _transform.Position with { Y = 2f };
    }

    public override unsafe void OnRender(double dt)
    {
        _shader.Use();
        _shader.SetUniform("uModel", _transform.ModelMatrix);
        _shader.SetUniform("uColor", Color.White);

        _vao.Bind();

        _gl.DrawElements(PrimitiveType.LineLoop, SegmentsCount, DrawElementsType.UnsignedInt, null);
    }

    private void GenerateAsteroidMesh()
    {
        List<float> vertices = new List<float>();
        List<uint> indices = new List<uint>();

        for (int i = 0; i < SegmentsCount; i++)
        {
            float angle = (float)i / SegmentsCount * MathF.PI * 2;

            // Случайное искажение радиуса
            float noise = 1.0f + ((float)_random.NextDouble() * 2 - 1) * Roughness;

            float x = MathF.Cos(angle) * noise;
            float y = MathF.Sin(angle) * noise;

            vertices.AddRange(new float[] { x, y, 0 });

            indices.Add((uint)i);
        }

        _vertices = vertices.ToArray();
        _indices = indices.ToArray();
    }
}