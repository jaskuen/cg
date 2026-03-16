using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL.Objects;
using SilkOpenGL.Store;

namespace SilkOpenGL.Text;

public class TextObject : RenderableObject, IText
{
    private new const string ShaderKey = "text";
    private const string FontMetadataKey = "font";

    private string _text;
    private float _fontSize;

    private Color _color;

    private FontMetadata _fontMetadata;

    public TextObject(Vector3 position, string text, float fontSize, Color color)
        : base(ShaderKey, ShaderKey)
    {
        _text = text;
        _fontSize = fontSize;
        _color = color;
        _transform = new Transform();
        _transform.Position = position;
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                UpdateMesh();
            }
        }
    }

    protected override void OnInit()
    {
    }

    private void UpdateMesh()
    {
        if (string.IsNullOrEmpty(_text) || _fontMetadata == null)
        {
            _vertices = Array.Empty<float>();
            _indices = Array.Empty<uint>();
            return;
        }

        var vertices = new List<float>();
        var indices = new List<uint>();

        float xCursor = 0;
        float yCursor = 0;

        // 1. Вычисляем итоговый масштаб. 
        // Предположим, _fontMetadata.BaseSize — это поле 'size' или 'lineHeight' из XML (обычно 32, 64 или 72).
        // Если в XML нет явного BaseSize, используй то число, под которое подгонял шрифт (например, 72f).
        float baseFontSize = _fontMetadata.BaseSize > 0 ? _fontMetadata.BaseSize : 72f;
        float finalScale = _fontSize / baseFontSize;

        for (int i = 0; i < _text.Length; i++)
        {
            char c = _text[i];

            if (!_fontMetadata.Chars.TryGetValue(c, out var fc))
            {
                // Пробел: используем xAdvance из метаданных, если есть, иначе половину fontSize
                float advance = (c == ' ')
                    ? (_fontMetadata.BaseSize * 0.25f)
                    : 20f;
                if (c == '\n')
                {
                    xCursor = 0;
                    yCursor -= advance * finalScale * 5f;
                }
                else
                {
                    xCursor += advance * finalScale;
                }

                continue;
            }

            // UV без изменений
            float u1 = (float)fc.X / _fontMetadata.TextureWidth;
            float v1 = (float)fc.Y / _fontMetadata.TextureHeight;
            float u2 = (float)(fc.X + fc.Width) / _fontMetadata.TextureWidth;
            float v2 = (float)(fc.Y + fc.Height) / _fontMetadata.TextureHeight;

            // 2. Применяем _fontSize через finalScale
            // Теперь xPos и yPos будут в мировых единицах. 
            // Если _fontSize = 1.0f, то заглавная буква будет высотой примерно в 1 единицу сетки.
            float xPos = xCursor + (fc.XOffset * finalScale);
            float yPos = yCursor - (fc.YOffset * finalScale);

            float w = fc.Width * finalScale;
            float h = fc.Height * finalScale;

            uint vIndex = (uint)(vertices.Count / 5);

            // Вершины квада (X, Y, Z, U, V)
            vertices.AddRange(new float[]
            {
                xPos, yPos - h, 0, u1, v2, // Лево-Низ
                xPos + w, yPos - h, 0, u2, v2, // Право-Низ
                xPos, yPos, 0, u1, v1, // Лево-Верх
                xPos + w, yPos, 0, u2, v1 // Право-Верх
            });

            indices.AddRange(new uint[]
            {
                vIndex, vIndex + 1, vIndex + 2,
                vIndex + 1, vIndex + 2, vIndex + 3
            });

            // Продвигаем курсор на расстояние, указанное в шрифте, с учетом нашего размера
            xCursor += fc.XAdvance * finalScale;
        }

        _vertices = vertices.ToArray();
        _indices = indices.ToArray();

        UpdateGpuBuffers();
    }

    private void UpdateGpuBuffers()
    {
        if (_vbo == null)
        {
            _vbo = new BufferObject<float>(_gl, _vertices, BufferTargetARB.ArrayBuffer);
            _ebo = new BufferObject<uint>(_gl, _indices, BufferTargetARB.ElementArrayBuffer);
            _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

            // Настройка VAO: 0 - Position (3f), 1 - TexCoords (2f). Stride = 5
            _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
            _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
        }
        else
        {
            // ВАЖНО: BufferObject.Update должен использовать glBufferData, 
            // чтобы размер буфера в видеопамяти мог вырасти при добавлении букв.
            _vbo.Update(_vertices);
            _ebo.Update(_indices);
        }
    }

    public override unsafe void OnRender(double dt)
    {
        _shader.Use();
        _texture!.Bind();

        _shader.SetUniform("uModel", _transform.ModelMatrix);
        _shader.SetUniform("uTexture", 0);
        _shader.SetUniform("uTextColor", _color);

        BindResources();
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Length, DrawElementsType.UnsignedInt, null);
    }

    public override void BindResources()
    {
        _vao.Bind();
        _ebo.Bind();
        _vbo.Bind();
    }

    public override void OnUpdate(double dt)
    {
    }

    public void SetMetadata(FontStore store)
    {
        _fontMetadata = store.GetMetadata(FontMetadataKey);
        UpdateMesh();
    }
}