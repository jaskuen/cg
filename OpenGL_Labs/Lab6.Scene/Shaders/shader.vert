#version 430 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;
layout (location = 3) in vec3 aTangent;
layout (location = 4) in vec3 aBitangent;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uNormalMatrix;

out vec3 vWorldPos;
out vec3 vWorldNormal;
out vec3 vWorldTangent;
out vec3 vWorldBitangent;
out vec2 vTexCoords;
out vec3 vViewPos;

void main()
{
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    vWorldPos = worldPos.xyz;
    
    mat3 normalTransform = mat3(uNormalMatrix);
    vWorldNormal = normalize(normalTransform * aNormal);
    vWorldTangent = normalTransform * aTangent;
    vWorldBitangent = normalTransform * aBitangent;
    vTexCoords = aTexCoords;
    
    // View position is the camera position in world space
    vViewPos = vec3(inverse(uView)[3]);
    
    gl_Position = uProjection * uView * worldPos;
}
