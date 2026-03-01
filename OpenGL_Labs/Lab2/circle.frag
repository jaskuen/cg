#version 330 core
in vec2 vPos;              // от -1 до 1 внутри квадрата

uniform vec4 uColor;  // цвет круга

out vec4 FragColor;

void main()
{
    // Расстояние от центра квадрата
    float dist = length(vPos) * 4;

    // Сглаживание с fwidth (resolution-independent AA)
    float width = fwidth(dist) * 1.5;                  // ширина пикселя в единицах расстояния
    float aaEdge = smoothstep(1.0 - width, 1.0 + width, dist);

    // Прозрачность: 1 внутри, 0 снаружи
    float alpha = 1.0 - aaEdge;

    if (alpha < 0.01) discard;                   // отсечение (оптимизация)

    FragColor = vec4(uColor.rgb, uColor.a * alpha);
}