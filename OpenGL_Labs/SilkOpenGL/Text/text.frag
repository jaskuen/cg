#version 330 core
in vec2 fTexCoords;
out vec4 FragColor;

uniform sampler2D uTexture;
uniform vec4 uTextColor; // Можно добавить Uniform для цвета текста

void main() {
    vec4 sampled = texture(uTexture, fTexCoords);
    // Если на атласе текст белый, а фон прозрачный:
    FragColor = uTextColor * sampled;
}