#version 430 core

in vec2 vTexCoords;
out vec4 FragColor;

uniform sampler2D uSkyTexture;

void main()
{
    FragColor = texture(uSkyTexture, vTexCoords);
}
