#version 330 core

in vec3 vWorldPosition;
in vec3 vWorldNormal;
in vec3 vColor;

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
    vec3 normal = normalize(vWorldNormal);

    if (uLightCount <= 0)
    {
        FragColor = vec4(vColor, 1.0);
        return;
    }

    vec3 litColor = vec3(0.0);
    for (int i = 0; i < uLightCount; i++)
    {
        vec3 toLight = uLights[i].position - vWorldPosition;
        float distanceToLight = length(toLight);
        vec3 lightDirection = normalize(toLight);

        float attenuation = 1.0 / (
            uLights[i].constant +
            uLights[i].linear * distanceToLight +
            uLights[i].quadratic * distanceToLight * distanceToLight);

        vec3 ambient = vColor * uLights[i].ambient;
        float diffuseFactor = abs(dot(normal, lightDirection));
        vec3 diffuse = vColor * uLights[i].diffuse * diffuseFactor * uLights[i].intensity;

        litColor += (ambient + diffuse) * attenuation;
    }

    FragColor = vec4(clamp(litColor, 0.0, 1.0), 1.0);
}
