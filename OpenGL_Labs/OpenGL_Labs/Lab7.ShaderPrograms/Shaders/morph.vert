#version 330 core

layout (location = 0) in vec3 aPosition;

uniform mat4 uModel;
uniform mat4 uNormalMatrix;
uniform mat4 uView;
uniform mat4 uProjection;
uniform float uMorph;
uniform vec3 uBaseColor;

out vec3 vWorldPosition;
out vec3 vWorldNormal;
out vec3 vColor;

const float PI = 3.14159265359;
const float DOMAIN_SIZE = 2.4;

// Координаты данной точки для Ленты Мебиуса
vec3 Mobius(float u01, float v01)
{
    float u = u01 * PI * 2;
    float v = mix(-0.34, 0.34, v01);
    float radius = 0.82;
    float halfU = 0.5 * u;

    return vec3(
        (radius + v * cos(halfU)) * cos(u),
        (radius + v * cos(halfU)) * sin(u),
        v * sin(halfU));
}

// Координаты данной точки для Бутылки Клейна
vec3 Klein(float u01, float v01)
{
    float u = u01 * PI * 2;
    float v = v01 * PI * 2 + 0.5 * PI;
    float radius = 0.7;
    float factor = 4.0 * radius * (1.0 - cos(u) * 0.5);

    float x;
    float y;
    if (u < PI)
    {
        x = 6.0 * cos(u) * (1.0 + sin(u)) + factor * cos(u) * cos(v);
        y = 16.0 * sin(u) + factor * sin(u) * cos(v);
    }
    else
    {
        x = 6.0 * cos(u) * (1.0 + sin(u)) - factor * cos(v);
        y = 16.0 * sin(u);
    }

    float z = factor * sin(v);
    return vec3(x, y, z) * 0.055;
}

vec3 SurfacePoint(float u01, float v01, bool useKlein)
{
    float finalV = useKlein
        ? fract(v01 + 1.0)
        : clamp(v01, 0.0, 1.0);

    return useKlein ? Klein(u01, finalV) : Mobius(u01, finalV);
}

vec3 SurfaceNormal(float u01, float v01, bool useKlein)
{
    float du = 1.0 / 160.0;
    float dv = 1.0 / 160.0;

    float vPrev = useKlein ? fract(v01 - dv + 1.0) : max(v01 - dv, 0.0);
    float vNext = useKlein ? fract(v01 + dv) : min(v01 + dv, 1.0);

    vec3 pUPrev = SurfacePoint(u01 - du, v01, useKlein);
    vec3 pUNext = SurfacePoint(u01 + du, v01, useKlein);
    vec3 pVPrev = SurfacePoint(u01, vPrev, useKlein);
    vec3 pVNext = SurfacePoint(u01, vNext, useKlein);

    return normalize(cross(pUNext - pUPrev, pVNext - pVPrev));
}

void main()
{
    float u01 = clamp(aPosition.x / DOMAIN_SIZE + 0.5, 0.0, 1.0);
    float v01 = clamp(aPosition.y / DOMAIN_SIZE + 0.5, 0.0, 1.0);
    float morph = smoothstep(0.0, 1.0, clamp(uMorph, 0.0, 1.0));

    vec3 mobiusPosition = Mobius(u01, v01);
    vec3 kleinPosition = Klein(u01, v01);
    vec3 localPosition = mix(mobiusPosition, kleinPosition, morph);

    vec3 mobiusNormal = SurfaceNormal(u01, v01, false);
    vec3 kleinNormal = SurfaceNormal(u01, v01, true);
    if (dot(mobiusNormal, kleinNormal) < 0.0)
    {
        kleinNormal = -kleinNormal;
    }

    vec3 localNormal = normalize(mix(mobiusNormal, kleinNormal, morph));

    vec4 worldPosition = uModel * vec4(localPosition, 1.0);
    vWorldPosition = worldPosition.xyz;
    vWorldNormal = normalize(mat3(uNormalMatrix) * localNormal);
    vColor = mix(uBaseColor, vec3(0.32, 0.76, 1.0), morph);

    gl_Position = uProjection * uView * worldPosition;
}
