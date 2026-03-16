#version 330 core
in vec2 frag_txCoord;

uniform sampler2D uTexture;
uniform vec4 uColor;

out vec4 out_color;

void main()
{
    out_color = texture(uTexture, frag_txCoord) * vec4(uColor.x, uColor.y, uColor.z, 0.5);
}