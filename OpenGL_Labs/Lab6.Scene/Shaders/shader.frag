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

    vec3 baseColor;
};
uniform Material uMaterial;

const float PI = 3.14159265359;

vec3 getNormalFromMap()
{
    if (uMaterial.hasNormalMap == 1) {
        vec3 tangentNormal = texture(sampler2D(uTextures[uMaterial.normalMap]), vTexCoords).xyz * 2.0 - 1.0;

        vec3 Q1  = dFdx(vWorldPos);
        vec3 Q2  = dFdy(vWorldPos);
        vec2 st1 = dFdx(vTexCoords);
        vec2 st2 = dFdy(vTexCoords);

        vec3 N   = normalize(vWorldNormal);
        vec3 T  = Q1*st2.t - Q2*st1.t;
        
        if (length(T) < 0.0001) {
            vec3 up = abs(N.y) < 0.999 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
            T = normalize(cross(up, N));
        } else {
            T = normalize(T);
        }
        
        T = normalize(T - dot(T, N) * N);
        vec3 B  = cross(N, T);
        mat3 TBN = mat3(T, B, N);

        return normalize(TBN * tangentNormal);
    }
    return normalize(vWorldNormal);
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness*roughness;
    float a2 = a*a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return nom / max(denom, 0.0000001);
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float nom   = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

void main()
{
    vec3 albedo = uMaterial.baseColor;
    if (uMaterial.hasAlbedoMap == 1) {
        albedo *= pow(texture(sampler2D(uTextures[uMaterial.albedoMap]), vTexCoords).rgb, vec3(2.2));
    }
    
    float metallic = 0.0;
    if (uMaterial.hasMetallicMap == 1) {
        metallic = texture(sampler2D(uTextures[uMaterial.metallicMap]), vTexCoords).r;
    }
    
    float roughness = 0.5;
    if (uMaterial.hasRoughnessMap == 1) {
        roughness = texture(sampler2D(uTextures[uMaterial.roughnessMap]), vTexCoords).r;
    }
    
    float ao = 1.0;
    if (uMaterial.hasAoMap == 1) {
        ao = texture(sampler2D(uTextures[uMaterial.aoMap]), vTexCoords).r;
    }

    vec3 N = getNormalFromMap();
    vec3 V = normalize(vViewPos - vWorldPos);

    vec3 F0 = vec3(0.04); 
    F0 = mix(F0, albedo, metallic);

    vec3 Lo = vec3(0.0);
    for(int i = 0; i < uLightCount; ++i) 
    {
        vec3 L = normalize(uLights[i].position - vWorldPos);
        vec3 H = normalize(V + L);
        
        float distance = length(uLights[i].position - vWorldPos);
        float attenuation = 1.0 / (uLights[i].constant + uLights[i].linear * distance + uLights[i].quadratic * (distance * distance));
        vec3 radiance = uLights[i].diffuse * uLights[i].intensity * attenuation;

        // Cook-Torrance BRDF
        float NDF = DistributionGGX(N, H, roughness);   
        float G   = GeometrySmith(N, V, L, roughness);      
        vec3 F    = fresnelSchlick(max(dot(H, V), 0.0), F0);
           
        vec3 numerator    = NDF * G * F; 
        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001; 
        vec3 specular = numerator / denominator;
        
        vec3 kS = F;
        vec3 kD = vec3(1.0) - kS;
        kD *= 1.0 - metallic;	  

        float NdotL = max(dot(N, L), 0.0);        
        Lo += (kD * albedo / PI + specular) * radiance * NdotL;
    }   

    vec3 ambient = vec3(0.03) * albedo * ao;
    
    // Add base ambient from lights if defined
    if (uLightCount > 0) {
        ambient += uLights[0].ambient * albedo * ao;
    }
    
    vec3 color = ambient + Lo;

    // HDR tonemapping
    color = color / (color + vec3(1.0));
    // Gamma correction
    color = pow(color, vec3(1.0/2.2)); 

    FragColor = vec4(color, 1.0);
}
