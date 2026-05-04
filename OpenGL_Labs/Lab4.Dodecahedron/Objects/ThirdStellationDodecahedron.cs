using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using SilkOpenGL;
using SilkOpenGL.Objects;

namespace Lab4.Dodecahedron.Objects;

public class ThirdStellationDodecahedron : RenderableObject
{
    private float _rotation;
    private readonly struct TriangleData
    {
        public readonly Vector3 A;
        public readonly Vector3 B;
        public readonly Vector3 C;
        public readonly Vector3 Normal;

        public TriangleData( Vector3 a, Vector3 b, Vector3 c, Vector3 normal )
        {
            A = a;
            B = b;
            C = c;
            Normal = normal;
        }
    }

    public ThirdStellationDodecahedron( string shaderKey ) : base( shaderKey )
    {
        _transform.Scale = new Vector3( 0.21f );
        _transform.Position = new Vector3( 0f, 0f, 0f );
    }

    protected override void OnInit()
    {
        BuildGeometry();

        _vbo = new BufferObject<float>( _gl, _vertices, BufferTargetARB.ArrayBuffer );
        _ebo = new BufferObject<uint>( _gl, _indices, BufferTargetARB.ElementArrayBuffer );
        _vao = new VertexArrayObject<float, uint>( _gl, _vbo, _ebo );
        _vao.VertexAttributePointer( 0, 3, VertexAttribPointerType.Float, 9, 0 );
        _vao.VertexAttributePointer( 1, 3, VertexAttribPointerType.Float, 9, 3 );
        _vao.VertexAttributePointer( 2, 3, VertexAttribPointerType.Float, 9, 6 );
    }

    public override void OnUpdate( double dt )
    {
    }

    public override unsafe void OnRender( double dt )
    {
        _gl.Enable( EnableCap.DepthTest );
        _gl.DepthMask( true );

        _gl.Enable( EnableCap.Blend );
        _gl.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
        _gl.Disable( EnableCap.CullFace );

        _shader.Use();
        _shader.SetUniform( "uModel", _transform.ModelMatrix );

        Matrix4x4.Invert( _transform.ModelMatrix, out var invModel );
        Matrix4x4 normalMatrix = Matrix4x4.Transpose( invModel );
        _shader.SetUniform( "uNormalMatrix", normalMatrix );

        _vao.Bind();

        _gl.PolygonMode( GLEnum.FrontAndBack, PolygonMode.Fill );
        _shader.SetUniform( "uUseVertexColor", 1 );
        _shader.SetUniform( "uColor", Color.FromArgb( 180, 200, 155, 255 ) );
        _gl.DrawElements( PrimitiveType.Triangles, ( uint )_indices.Length, DrawElementsType.UnsignedInt, null );

        _gl.PolygonMode( GLEnum.FrontAndBack, PolygonMode.Line );
        _gl.LineWidth( 1.5f );

        _gl.Enable( EnableCap.PolygonOffsetLine );
        _gl.PolygonOffset( -1.0f, -1.0f );

        _shader.SetUniform( "uUseVertexColor", 0 );
        _shader.SetUniform( "uColor", Color.Black );
        _gl.DrawElements( PrimitiveType.Triangles, ( uint )_indices.Length, DrawElementsType.UnsignedInt, null );

        _gl.Disable( EnableCap.PolygonOffsetLine );
        _gl.BindVertexArray( 0 );
    }

    private void BuildGeometry()
    {
        // Вершины внутреннего ядра (Икосаэдра)
        List<Vector3> coreVertices = BuildIcosahedronVertices();
        List<int[]> coreFaces = BuildIcosahedronFaces();
        List<TriangleData> triangles = [];

        // Золотое сечение для расчета высоты пиков
        float phi = ( 1.0f + MathF.Sqrt( 5.0f ) ) / 2.0f;

        // Коэффициент высоты для Большого звёздчатого додекаэдра
        // Чтобы вершины пирамид совпали с вершинами додекаэдра, 
        // высота должна соотноситься с радиусом икосаэдра определенным образом.
        // Для GSD это расстояние от центра до вершины додекаэдра.
        float tipScale = phi * phi; // Соотношение внешнего радиуса к внутреннему

        for ( int faceIndex = 0; faceIndex < coreFaces.Count; faceIndex++ )
        {
            int[] face = coreFaces[faceIndex];

            // Вершины грани икосаэдра
            Vector3 v1 = coreVertices[face[0]];
            Vector3 v2 = coreVertices[face[1]];
            Vector3 v3 = coreVertices[face[2]];

            // Находим центр грани и нормаль
            Vector3 center = ( v1 + v2 + v3 ) / 3.0f;
            Vector3 normal = Vector3.Normalize( center );

            // Вершина пика (Apex). 
            // В GSD пики очень длинные. Мы берем направление из центра и масштабируем.
            Vector3 apex = normal * ( center.Length() * tipScale );

            // Каждая грань икосаэдра превращается в 3 треугольника пирамиды
            AddOrientedTriangle( triangles, v1, v2, apex );
            AddOrientedTriangle( triangles, v2, v3, apex );
            AddOrientedTriangle( triangles, v3, v1, apex );
        }

        List<Vector3> triangleColors = BuildTriangleColors( triangles );
        List<float> meshVertices = [];
        List<uint> meshIndices = [];

        for ( int i = 0; i < triangles.Count; i++ )
        {
            TriangleData triangle = triangles[i];
            AddTriangle( meshVertices, meshIndices, triangle.A, triangle.B, triangle.C, triangle.Normal, triangleColors[i] );
        }

        _vertices = meshVertices.ToArray();
        _indices = meshIndices.ToArray();
    }

    private List<Vector3> BuildIcosahedronVertices()
    {
        float phi = ( 1.0f + MathF.Sqrt( 5.0f ) ) / 2.0f;
        return new List<Vector3>
        {
            new( -1, phi, 0 ), new( 1, phi, 0 ), new( -1, -phi, 0 ), new( 1, -phi, 0 ),
            new( 0, -1, phi ), new( 0, 1, phi ), new( 0, -1, -phi ), new( 0, 1, -phi ),
            new( phi, 0, -1 ), new( phi, 0, 1 ), new( -phi, 0, -1 ), new( -phi, 0, 1 )
        };
    }

    private List<int[]> BuildIcosahedronFaces()
    {
        return new List<int[]>
        {
            new[] { 0, 11, 5 }, new[] { 0, 5, 1 }, new[] { 0, 1, 7 }, new[] { 0, 7, 10 }, new[] { 0, 10, 11 },
            new[] { 1, 5, 9 }, new[] { 5, 11, 4 }, new[] { 11, 10, 2 }, new[] { 10, 7, 6 }, new[] { 7, 1, 8 },
            new[] { 3, 9, 4 }, new[] { 3, 4, 2 }, new[] { 3, 2, 6 }, new[] { 3, 6, 8 }, new[] { 3, 8, 9 },
            new[] { 4, 9, 5 }, new[] { 2, 4, 11 }, new[] { 6, 2, 10 }, new[] { 8, 6, 7 }, new[] { 9, 8, 1 }
        };
    }

    private static void AddTriangle( List<float> vertices, List<uint> indices, Vector3 a, Vector3 b, Vector3 c,
        Vector3 normal, Vector3 color )
    {
        uint startIndex = ( uint )( vertices.Count / 9 );
        AddVertex( vertices, a, normal, color );
        AddVertex( vertices, b, normal, color );
        AddVertex( vertices, c, normal, color );

        indices.Add( startIndex );
        indices.Add( startIndex + 1 );
        indices.Add( startIndex + 2 );
    }

    private static void AddVertex( List<float> vertices, Vector3 pos, Vector3 normal, Vector3 color )
    {
        vertices.Add( pos.X );
        vertices.Add( pos.Y );
        vertices.Add( pos.Z );
        vertices.Add( normal.X );
        vertices.Add( normal.Y );
        vertices.Add( normal.Z );
        vertices.Add( color.X );
        vertices.Add( color.Y );
        vertices.Add( color.Z );
    }

    private static void AddOrientedTriangle( List<TriangleData> triangles, Vector3 a, Vector3 b, Vector3 c )
    {
        Vector3 normal = Vector3.Normalize( Vector3.Cross( b - a, c - a ) );
        if ( float.IsNaN( normal.X ) || normal.LengthSquared() < 0.001f )
        {
            return;
        }

        Vector3 triangleCenter = ( a + b + c ) / 3f;
        if ( Vector3.Dot( normal, triangleCenter ) < 0f )
        {
            ( b, c ) = ( c, b );
            normal = -normal;
        }

        triangles.Add( new TriangleData( a, b, c, normal ) );
    }

    private static List<Vector3> BuildTriangleColors( List<TriangleData> triangles )
    {
        // Soft palette so adjacent triangles are visibly distinct.
        Vector3[] palette =
        [
            new(0.95f, 0.45f, 0.45f),
            new(0.45f, 0.85f, 0.55f),
            new(0.45f, 0.65f, 0.95f),
            new(0.95f, 0.75f, 0.35f),
            new(0.8f, 0.55f, 0.95f)
        ];

        List<int>[] adjacency = BuildTriangleAdjacency( triangles );
        int[] colorIndices = Enumerable.Repeat( -1, triangles.Count ).ToArray();

        for ( int i = 0; i < triangles.Count; i++ )
        {
            HashSet<int> used = [];
            foreach ( int neighbor in adjacency[i] )
            {
                if ( colorIndices[neighbor] >= 0 )
                {
                    used.Add( colorIndices[neighbor] );
                }
            }

            int selected = 0;
            while ( selected < palette.Length && used.Contains( selected ) )
            {
                selected++;
            }

            colorIndices[i] = selected % palette.Length;
        }

        return colorIndices.Select( idx => palette[idx] ).ToList();
    }

    private static List<int>[] BuildTriangleAdjacency( List<TriangleData> triangles )
    {
        List<int>[] adjacency = Enumerable.Range( 0, triangles.Count ).Select( _ => new List<int>() ).ToArray();
        for ( int i = 0; i < triangles.Count; i++ )
        {
            for ( int j = i + 1; j < triangles.Count; j++ )
            {
                if ( ShareEdge( triangles[i], triangles[j] ) )
                {
                    adjacency[i].Add( j );
                    adjacency[j].Add( i );
                }
            }
        }

        return adjacency;
    }

    private static bool ShareEdge( TriangleData a, TriangleData b )
    {
        int sharedVertices = 0;

        Vector3[] av = [a.A, a.B, a.C];
        Vector3[] bv = [b.A, b.B, b.C];

        for ( int i = 0; i < av.Length; i++ )
        {
            for ( int j = 0; j < bv.Length; j++ )
            {
                if ( NearlyEqual( av[i], bv[j] ) )
                {
                    sharedVertices++;
                }
            }
        }

        return sharedVertices >= 2;
    }

    private static bool NearlyEqual( Vector3 left, Vector3 right )
    {
        const float epsilon = 0.0001f;
        return MathF.Abs( left.X - right.X ) < epsilon &&
               MathF.Abs( left.Y - right.Y ) < epsilon &&
               MathF.Abs( left.Z - right.Z ) < epsilon;
    }
}