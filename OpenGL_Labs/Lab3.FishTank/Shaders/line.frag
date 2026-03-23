#version 330 core

uniform vec4 uColor = vec4(1.0, 1.0, 1.0, 1.0);
in vec3 vWorldPos;

struct Light
{
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
    vec3 baseColor = uColor.rgb;

    if (uLightCount <= 0)
    {
        FragColor = uColor;
        return;
    }

    vec3 lighting = vec3(0.0);
    for (int i = 0; i < uLightCount; i++)
    {
        float distance = length(uLights[i].position - vWorldPos);
        float attenuation = 1.0 / (uLights[i].constant + uLights[i].linear * distance +
                                   uLights[i].quadratic * distance * distance);
        vec3 lightContribution = uLights[i].ambient + (uLights[i].diffuse * attenuation * uLights[i].intensity);
        lighting += baseColor * lightContribution;
    }

    FragColor = vec4(clamp(lighting, 0.0, 1.0), uColor.a);
}