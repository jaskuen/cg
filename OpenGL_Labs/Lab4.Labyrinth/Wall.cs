using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab4.Labyrinth;

public class Wall : RenderableObject
{
    public WallType Type { get; }
    
    public Wall(Vector3 position, WallType type, string shaderKey, string materialKey) 
        : base(shaderKey, materialKey, isMaterial: true)
    {
        Type = type;
        _transform.Position = position;
        _transform.Scale = new Vector3(1f); // 1x1x1 cube
    }

    protected override void OnInit()
    {
        InitVertices();
        
        _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
        
        // PBR vertices: position (3), normal (3), texCoords (2) -> stride = 8 * sizeof(float)
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 8, 3);
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, 8, 6);
    }

    public override void OnUpdate(double dt)
    {
    }

    public override unsafe void OnRender(double dt)
    {
        _gl.Enable(EnableCap.DepthTest);
        
        _vao.Bind();
        _shader.Use();
        _shader.SetUniform("uModel", _transform.ModelMatrix);

        if (Matrix4x4.Invert(_transform.ModelMatrix, out var invModel))
        {
            Matrix4x4 normalMatrix = Matrix4x4.Transpose(invModel);
            _shader.SetUniform("uNormalMatrix", normalMatrix);
        }
        else
        {
            _shader.SetUniform("uNormalMatrix", _transform.ModelMatrix);
        }
        
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private void InitVertices()
    {
        float s = 0.5f; // half size
        
        _vertices = new float[]
        {
            // Front face
            -s, -s,  s,  0.0f,  0.0f,  1.0f,  0.0f, 0.0f,
             s, -s,  s,  0.0f,  0.0f,  1.0f,  1.0f, 0.0f,
             s,  s,  s,  0.0f,  0.0f,  1.0f,  1.0f, 1.0f,
            -s,  s,  s,  0.0f,  0.0f,  1.0f,  0.0f, 1.0f,

            // Back face
            -s, -s, -s,  0.0f,  0.0f, -1.0f,  1.0f, 0.0f,
            -s,  s, -s,  0.0f,  0.0f, -1.0f,  1.0f, 1.0f,
             s,  s, -s,  0.0f,  0.0f, -1.0f,  0.0f, 1.0f,
             s, -s, -s,  0.0f,  0.0f, -1.0f,  0.0f, 0.0f,

            // Left face
            -s, -s, -s, -1.0f,  0.0f,  0.0f,  0.0f, 0.0f,
            -s, -s,  s, -1.0f,  0.0f,  0.0f,  1.0f, 0.0f,
            -s,  s,  s, -1.0f,  0.0f,  0.0f,  1.0f, 1.0f,
            -s,  s, -s, -1.0f,  0.0f,  0.0f,  0.0f, 1.0f,

            // Right face
             s, -s, -s,  1.0f,  0.0f,  0.0f,  1.0f, 0.0f,
             s,  s, -s,  1.0f,  0.0f,  0.0f,  1.0f, 1.0f,
             s,  s,  s,  1.0f,  0.0f,  0.0f,  0.0f, 1.0f,
             s, -s,  s,  1.0f,  0.0f,  0.0f,  0.0f, 0.0f,
        };

        _indices = new uint[]
        {
            0, 1, 2,  2, 3, 0,       // Front
            4, 5, 6,  6, 7, 4,       // Back
            8, 9, 10, 10, 11, 8,     // Left
            12, 13, 14, 14, 15, 12,  // Right
        };
    }
}