#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aSize;
layout (location = 2) in float aRotation;
layout (location = 3) in float aKind;

out VS_OUT
{
    vec2 size;
    float rotation;
    float kind;
} vs_out;

void main()
{
    gl_Position = vec4(aPosition, 1.0);
    vs_out.size = aSize;
    vs_out.rotation = aRotation;
    vs_out.kind = aKind;
}
