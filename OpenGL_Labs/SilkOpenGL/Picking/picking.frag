#version 330 core
uniform vec3 uPickingColor;
out vec4 FragColor;

void main() {
    FragColor = vec4(uPickingColor, 1.0);
}