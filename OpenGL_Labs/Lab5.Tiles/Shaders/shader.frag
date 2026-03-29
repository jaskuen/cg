#version 430 core
#extension GL_ARB_bindless_texture : require

in vec3 frag_txCoord;

layout(std430, binding = 0) buffer TextureBuffer {
    uvec2 uTextures[];
};

uniform vec4 uColor;
uniform bool uHasTexture;
uniform int uHandle;

out vec4 out_color;

void main()
{
    if (uHasTexture && frag_txCoord.z > 0)
    {
        out_color = texture(sampler2D(uTextures[uHandle]), frag_txCoord.xy) * vec4(1.0, 1.0, 1.0, 1.0);
        if (out_color.a < 0.001)
        {
            out_color = vec4(1.0, 1.0, 1.0, 1.0);
        }
    }
    else
    {
        out_color = uColor;
    }
}