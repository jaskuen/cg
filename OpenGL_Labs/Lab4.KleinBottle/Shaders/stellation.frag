#version 330 core

in vec3 vWorldPos;
in vec3 vWorldNormal;
in vec3 vViewPos; // Получено из вертексного шейдера
in vec3 vFaceColor;

uniform vec4 uColor = vec4(0.9, 0.7, 1.0, 0.5);
uniform int uUseVertexColor = 1;

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

out vec4 FragColor;

void main()
{
    vec3 baseColor = (uUseVertexColor == 1) ? vFaceColor : uColor.rgb;
    vec3 normal = normalize(vWorldNormal);
    vec3 viewDir = normalize(vViewPos - vWorldPos);

    if (dot(normal, viewDir) < 0.0) {
        normal = -normal;
    }

    if (uLightCount <= 0) {
        FragColor = uColor;
        return;
    }

    vec3 litColor = vec3(0.0);

    for (int i = 0; i < uLightCount; i++)
    {
        vec3 toLight = uLights[i].position - vWorldPos;
        float distanceToLight = length(toLight);
        vec3 lightDir = normalize(toLight);

        // Затухание
        float attenuation = 1.0 / (uLights[i].constant +
                                   uLights[i].linear * distanceToLight +
                                   uLights[i].quadratic * distanceToLight * distanceToLight);

        // 1. Ambient
        vec3 ambient = baseColor * uLights[i].ambient;

        // 2. Diffuse
        float diffuseFactor = max(dot(normal, lightDir), 0.0);
        vec3 diffuse = baseColor * uLights[i].diffuse * diffuseFactor * uLights[i].intensity;

        // 3. Specular (Блики) - делают грани "металлическими" и острыми
        vec3 reflectDir = reflect(-lightDir, normal);
        // Коэффициент 32.0 - это размер блика (чем больше, тем меньше и острее точка)
        float specFactor = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
        vec3 specular = uLights[i].specular * specFactor;

        litColor += (ambient + diffuse + specular) * attenuation;
    }

    FragColor = vec4(clamp(litColor, 0.0, 1.0), uColor.a);
}