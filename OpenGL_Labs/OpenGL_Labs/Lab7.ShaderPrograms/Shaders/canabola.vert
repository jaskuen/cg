#version 330 core

layout (location = 0) in vec3 aPosition;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform float uScale = 1.0;

void main()
{
    float x = aPosition.x;
    float r = (1.0 + sin(x))
        * (1.0 + 0.9 * cos(8.0 * x))
        * (1.0 + 0.1 * cos(24.0 * x))
        * (0.5 + 0.05 * cos(140.0 * x));

    vec3 curvedPosition = vec3(
        r * cos(x) * uScale,
        r * sin(x) * uScale,
        aPosition.z);

    gl_Position = uProjection * uView * uModel * vec4(curvedPosition, 1.0);
}
