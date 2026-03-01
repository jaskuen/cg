#version 330 core
layout(location = 0) in vec2 aPosition;

uniform mat4 uModel;       // позиция + масштаб круга
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 vPos;             // локальная позиция [-1..1]

void main()
{
    vPos = aPosition;
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 0.0, 1.0);
}