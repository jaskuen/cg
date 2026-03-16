#version 330 core
in vec2 vPos;
in vec2 frag_txCoord;

uniform sampler2D uTexture;
uniform vec4 uColor;

out vec4 FragColor;

void main()
{
    // Расстояние от центра квадрата
    float dist = length(vPos);

    // Сглаживание с fwidth (resolution-independent AA)
    float width = fwidth(dist) * 1.5;                  // ширина пикселя в единицах расстояния
    float aaEdge = smoothstep(1.0 - width, 1.0 + width, dist);

    // Прозрачность: 1 внутри, 0 снаружи
    float alpha = 1.0 - aaEdge;

    if (alpha < 0.01) discard;                   // отсечение (оптимизация)

//    FragColor = vec4(frag_txCoord.x, frag_txCoord.y, 0, alpha);
    FragColor = texture(uTexture, frag_txCoord) * vec4(uColor.x, uColor.y, uColor.z, alpha);
}