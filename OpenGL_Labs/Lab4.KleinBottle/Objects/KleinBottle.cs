using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab4.KleinBottle.Objects;

public class KleinBottle : RenderableObject
{
    private int _parts = 100;
    private float _radius = 0.7f;
    private Color _baseColor = Color.FromArgb(255, 200, 155, 255);
    
    public KleinBottle(string shaderKey) : base(shaderKey)
    {
    }

    public override void OnUpdate(double dt)
    {
    }

    public override unsafe void OnRender(double dt)
    {
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthMask(true);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        // Klein bottle is a non-orientable surface, so we must disable culling
        _gl.Disable(EnableCap.CullFace);

        _shader.Use();
        _shader.SetUniform("uModel", _transform.ModelMatrix);

        if (Matrix4x4.Invert(_transform.ModelMatrix, out var invModel))
        {
            Matrix4x4 normalMatrix = Matrix4x4.Transpose(invModel);
            _shader.SetUniform("uNormalMatrix", normalMatrix);
        }

        _shader.SetUniform("uUseVertexColor", 1);
        _shader.SetUniform("uColor", _baseColor);

        _vao.Bind();
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    protected override void OnInit()
    {
        InitVertices();

        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        
        // Layout: Pos(3), Normal(3), Color(3)
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 9, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 9, 3);
        _vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 9, 6);

        _transform.Position = new Vector3(0, 0, 0);
        _transform.Scale = new Vector3(0.05f, -0.05f, 0.05f);
    }

    private void InitVertices()
    {
        float deltaU = 2 * MathF.PI / _parts;
        float deltaV = 2 * MathF.PI / _parts;

        int vertexCount = (_parts + 1) * (_parts + 1);
        Vector3[] positions = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];

        // 1. Generate positions
        for (int i = 0; i <= _parts; i++)
        {
            float u = i * deltaU;
            for (int j = 0; j <= _parts; j++)
            {
                float v = j * deltaV;
                float x, y, z;

                float factor = 4 * _radius * (1 - MathF.Cos(u) / 2);
                if (u <= MathF.PI)
                {
                    x = 6 * MathF.Cos(u) * (1 + MathF.Sin(u)) + factor * MathF.Cos(u) * MathF.Cos(v);
                    y = 16 * MathF.Sin(u) + factor * MathF.Sin(u) * MathF.Cos(v);
                }
                else
                {
                    x = 6 * MathF.Cos(u) * (1 + MathF.Sin(u)) - factor * MathF.Cos(v);
                    y = 16 * MathF.Sin(u);
                }
                z = factor * MathF.Sin(v);

                positions[i * (_parts + 1) + j] = new Vector3(x, y, z);
            }
        }

        // 2. Generate indices and calculate face normals for averaging
        List<uint> indicesList = [];
        for (int i = 0; i < _parts; i++)
        {
            for (int j = 0; j < _parts; j++)
            {
                uint p1 = (uint)(i * (_parts + 1) + j);
                uint p2 = (uint)((i + 1) * (_parts + 1) + j);
                uint p3 = (uint)((i + 1) * (_parts + 1) + (j + 1));
                uint p4 = (uint)(i * (_parts + 1) + (j + 1));

                // Triangle 1
                indicesList.Add(p1);
                indicesList.Add(p2);
                indicesList.Add(p3);

                Vector3 v1 = positions[p1];
                Vector3 v2 = positions[p2];
                Vector3 v3 = positions[p3];
                Vector3 n1 = Vector3.Cross(v2 - v1, v3 - v1);
                normals[p1] += n1;
                normals[p2] += n1;
                normals[p3] += n1;

                // Triangle 2
                indicesList.Add(p1);
                indicesList.Add(p3);
                indicesList.Add(p4);

                Vector3 v4 = positions[p4];
                Vector3 n2 = Vector3.Cross(v3 - v1, v4 - v1);
                normals[p1] += n2;
                normals[p3] += n2;
                normals[p4] += n2;
            }
        }
        
        List<float> finalVertices = [];
        
        Vector3 colorVec = new Vector3(_baseColor.R / 255f, _baseColor.G / 255f, _baseColor.B / 255f);
        
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 pos = positions[i];
            Vector3 norm = normals[i].LengthSquared() > 0 ? Vector3.Normalize(normals[i]) : Vector3.UnitY;

            finalVertices.Add(pos.X);
            finalVertices.Add(pos.Y);
            finalVertices.Add(pos.Z);
            finalVertices.Add(norm.X);
            finalVertices.Add(norm.Y);
            finalVertices.Add(norm.Z);
            finalVertices.Add(colorVec.X);
            finalVertices.Add(colorVec.Y);
            finalVertices.Add(colorVec.Z);
        }

        _vertices = [.. finalVertices];
        _indices = [.. indicesList];
    }
}