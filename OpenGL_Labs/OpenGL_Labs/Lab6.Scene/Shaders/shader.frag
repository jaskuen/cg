#version 430 core
#extension GL_ARB_bindless_texture : require

in vec3 vWorldPos;
in vec3 vWorldNormal;
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
};
uniform Material uMaterial;

vec3 sampleAlbedo()
{
    if (uMaterial.hasAlbedoMap == 1) {
        return pow(texture(sampler2D(uTextures[uMaterial.albedoMap]), vTexCoords).rgb, vec3(2.2));
    }

    return vec3(1.0);
}

void main()
{
    vec3 albedo = sampleAlbedo();
    vec3 normal = normalize(vWorldNormal);
    vec3 viewDir = normalize(vViewPos - vWorldPos);
    vec3 color = vec3(0.0);

    for (int i = 0; i < uLightCount; i++) {
        Light light = uLights[i];
        vec3 lightVector = light.position - vWorldPos;
        float distanceToLight = length(lightVector);
        vec3 lightDir = distanceToLight > 0.0 ? lightVector / distanceToLight : vec3(0.0, 0.0, 1.0);

        float attenuation = 1.0 / (light.constant + light.linear * distanceToLight + light.quadratic * distanceToLight * distanceToLight);
        attenuation *= light.intensity;

        float diffuseStrength = max(dot(normal, lightDir), 0.0);
        vec3 halfwayDir = normalize(lightDir + viewDir);
        float specularStrength = pow(max(dot(normal, halfwayDir), 0.0), 32.0);

        vec3 ambient = light.ambient * albedo;
        vec3 diffuse = light.diffuse * diffuseStrength * albedo;
        vec3 specular = light.specular * specularStrength * 0.25;

        color += (ambient + diffuse + specular) * attenuation;
    }

    if (uLightCount == 0) {
        color = albedo;
    }

    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0 / 2.2));

    FragColor = vec4(color, 1.0);
}
