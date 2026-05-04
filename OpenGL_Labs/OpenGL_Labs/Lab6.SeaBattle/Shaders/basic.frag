#version 430 core
#extension GL_ARB_bindless_texture : require

in vec3 vWorldPos;
in vec3 vNormal;
in vec2 vTexCoords;
in vec3 vViewPos;

out vec4 FragColor;

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float intensity;
    float constant;
    float linear;
    float quadratic;
};

uniform int uLightCount = 0;
uniform Light uLights[8];
uniform vec3 uColor = vec3(0.8);
uniform float uRoughness = 0.45;
uniform float uMetallic = 0.0;
uniform float uAlpha = 1.0;

layout(std430, binding = 0) buffer TextureBuffer {
    uvec2 uTextures[];
};

struct Material {
    int albedoMap;
    int normalMap;
    int metallicMap;
    int roughnessMap;
    int aoMap;
    int hasAlbedoMap;
    int hasNormalMap;
    int hasMetallicMap;
    int hasRoughnessMap;
    int hasAoMap;
    vec3 baseColor;
};
uniform Material uMaterial;

void main()
{
    vec3 N = normalize(vNormal);
    vec3 V = normalize(vViewPos - vWorldPos);
    float hasImportedColor = step(0.001, dot(uMaterial.baseColor, uMaterial.baseColor));
    vec3 baseColor = mix(uColor, uMaterial.baseColor, hasImportedColor);
    if (uMaterial.hasAlbedoMap == 1) {
        baseColor *= texture(sampler2D(uTextures[uMaterial.albedoMap]), vTexCoords).rgb;
    }
    vec3 color = vec3(0.035, 0.045, 0.06) * baseColor;

    for (int i = 0; i < uLightCount; i++)
    {
        vec3 L = normalize(uLights[i].position - vWorldPos);
        vec3 H = normalize(L + V);
        float distance = length(uLights[i].position - vWorldPos);
        float attenuation = 1.0 / (uLights[i].constant + uLights[i].linear * distance + uLights[i].quadratic * distance * distance);
        float diffuse = max(dot(N, L), 0.0);
        float shininess = mix(96.0, 18.0, clamp(uRoughness, 0.0, 1.0));
        float specular = pow(max(dot(N, H), 0.0), shininess) * mix(0.25, 0.85, uMetallic);
        vec3 radiance = uLights[i].diffuse * uLights[i].intensity * attenuation;
        color += uLights[i].ambient * baseColor;
        color += (baseColor * diffuse + uLights[i].specular * specular) * radiance;
    }

    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0 / 2.2));
    FragColor = vec4(color, uAlpha);
}
