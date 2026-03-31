#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTxCoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 frag_txCoord;

void main()
{
    frag_txCoord = aTxCoord;
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
}