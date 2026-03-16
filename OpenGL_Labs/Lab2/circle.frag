#version 330 core
in vec2 vPos;

uniform vec4 uColor;

out vec4 FragColor;

void main()
{
    float dist = length(vPos) * 4;

    float width = fwidth(dist) * 1.5;                  // ширина пикселя в единицах расстояния
    float aaEdge = smoothstep(1.0 - width, 1.0 + width, dist);

    // Прозрачность: 1 внутри, 0 снаружи
    float alpha = 1.0 - aaEdge;

    if (alpha < 0.01) discard;                   // отсечение (оптимизация)

    FragColor = vec4(uColor.rgb, uColor.a * alpha);
}