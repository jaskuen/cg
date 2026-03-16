#version 330 core
in vec2 fTexCoords;
out vec4 FragColor;

uniform sampler2D uTexture;
uniform vec4 uTextColor;

void main() {
    vec4 sampled = texture(uTexture, fTexCoords);
    FragColor = uTextColor * sampled;
}