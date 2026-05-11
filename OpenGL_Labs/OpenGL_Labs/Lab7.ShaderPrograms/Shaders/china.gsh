#version 330 core

layout (points) in;
layout (triangle_strip, max_vertices = 30) out;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

in VS_OUT
{
    vec2 size;
    float rotation;
    float kind;
} gs_in[];

out vec4 gColor;

const float PI = 3.14159265359;
const float INNER_STAR_RADIUS = 0.38196601125;
const vec4 FLAG_RED = vec4(0.8706, 0.1608, 0.0627, 1.0);
const vec4 FLAG_YELLOW = vec4(1.0, 0.8706, 0.0, 1.0);

// Функция задания точки
void EmitPoint(vec3 position, vec4 color)
{
    gColor = color;
    gl_Position = uProjection * uView * uModel * vec4(position, 1.0);
    EmitVertex();
}

// Функция задания треугольника по трем точкам
void EmitTriangle(vec3 a, vec3 b, vec3 c, vec4 color)
{
    EmitPoint(a, color);
    EmitPoint(b, color);
    EmitPoint(c, color);
    EndPrimitive();
}

// Функция задания прямоугольника по центру и размерам
void EmitRectangle()
{
    vec3 center = gl_in[0].gl_Position.xyz;
    vec2 halfSize = gs_in[0].size * 0.5;

    vec3 topLeft = center + vec3(-halfSize.x, halfSize.y, 0.0);
    vec3 bottomLeft = center + vec3(-halfSize.x, -halfSize.y, 0.0);
    vec3 bottomRight = center + vec3(halfSize.x, -halfSize.y, 0.0);
    vec3 topRight = center + vec3(halfSize.x, halfSize.y, 0.0);

    EmitTriangle(topLeft, bottomLeft, bottomRight, FLAG_RED);
    EmitTriangle(topLeft, bottomRight, topRight, FLAG_RED);
}

// Функция задания одной из точек для будущей звезды
vec3 StarVertex(vec3 center, float outerRadius, float rotation, int index)
{
    float radius = index % 2 == 0
        ? outerRadius
        : outerRadius * INNER_STAR_RADIUS;
    float angle = rotation + float(index) * PI / 5.0;

    return center + vec3(cos(angle) * radius, sin(angle) * radius, 0.0);
}

// Функция рисования звезды
void EmitStar()
{
    vec3 center = gl_in[0].gl_Position.xyz;
    float outerRadius = gs_in[0].size.x;
    float rotation = gs_in[0].rotation;

    for (int i = 0; i < 10; i++)
    {
        vec3 current = StarVertex(center, outerRadius, rotation, i);
        vec3 next = StarVertex(center, outerRadius, rotation, (i + 1) % 10);
        EmitTriangle(center, current, next, FLAG_YELLOW);
    }
}

void main()
{
    if (gs_in[0].kind < 0.5)
    {
        EmitRectangle();
    }
    else
    {
        EmitStar();
    }
}
