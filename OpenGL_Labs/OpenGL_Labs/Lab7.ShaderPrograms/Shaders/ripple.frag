#version 330 core

in vec2 vTexCoords;

uniform sampler2D uSourceImage;
uniform sampler2D uTargetImage;
uniform float uProgress;
uniform vec3 uOrigin;

uniform int uFromTarget;

out vec4 FragColor;

float MaxRippleRadius(vec2 origin)
{
    vec2 farCorner = max(origin, 1.0 - origin);
    return length(farCorner);
}

void main()
{
    vec2 origin = uOrigin.xy;

    // Вектор от центра к текущему пикселю
    vec2 toPixel = vTexCoords - origin;

    // Расстояние до центра
    float distanceToOrigin = length(toPixel);

    // Нормализованное направление
    vec2 direction =
        distanceToOrigin > 0.0001
        ? toPixel / distanceToOrigin
        : vec2(0.0, 1.0);

    // Текущий радиус фронта волны
    float maxRadius = MaxRippleRadius(origin) + 0.18;
    float front = uProgress * maxRadius;

    // Расстояние от пикселя до фронта волны
    float distanceToFront = distanceToOrigin - front;

    // Маска кольца вокруг фронта
    float ring = smoothstep(0.18, 0.0, abs(distanceToFront));

    // Синусоидальная рябь
    float oscillation = sin(distanceToFront * 92.0);

    // Постепенное затухание эффекта к концу
    float fadeOut = 1.0 - smoothstep(0.76, 1.0, uProgress);

    // Смещение UV для ripple-эффекта
    vec2 rippleOffset =
        direction *
        oscillation *
        ring *
        0.035 *
        fadeOut;

    // Искажённые UV координаты
    vec2 warpedUv =
        clamp(vTexCoords + rippleOffset,
              vec2(0.0),
              vec2(1.0));

    vec4 imageA = texture(uSourceImage, warpedUv);
    vec4 imageB = texture(uTargetImage, warpedUv);

    vec4 fromColor = uFromTarget == 1 ? imageB : imageA;
    vec4 toColor   = uFromTarget == 1 ? imageA : imageB;

    // Маска раскрытия новой картинки
    float reveal =
        1.0 - smoothstep(front - 0.08,
                         front + 0.04,
                         distanceToOrigin);

    reveal = max(reveal,
                 smoothstep(0.92, 1.0, uProgress));

    // Подсветка фронта волны
    float brightness =
        1.0 +
        oscillation * ring * 0.28 * fadeOut;

    vec4 color = mix(fromColor, toColor, reveal);
    color.rgb *= brightness;

    FragColor =
        vec4(clamp(color.rgb, 0.0, 1.0), 1.0);
}