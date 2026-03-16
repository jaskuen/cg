#version 330 core
layout(location = 0) in vec2 aPosition;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 vPos;

void main()
{
    vPos = aPosition;
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 0.0, 1.0);
}