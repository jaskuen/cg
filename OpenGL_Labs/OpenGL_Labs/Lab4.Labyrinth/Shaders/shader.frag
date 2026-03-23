#version 330 core
in vec2 frag_txCoord;

uniform sampler2D uTexture;
uniform vec4 uColor;
uniform bool uHasTexture;

out vec4 out_color;

void main()
{
    if (uHasTexture)
    {
        out_color = texture(uTexture, frag_txCoord) * uColor;
    }
    else
    {
        out_color = uColor;
    }
}