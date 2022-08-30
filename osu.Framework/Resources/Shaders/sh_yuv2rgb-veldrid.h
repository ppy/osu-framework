#include "sh_TextureWrapping.h"

layout(set = 1, binding = 0) uniform texture2D m_TextureY;
layout(set = 1, binding = 1) uniform texture2D m_TextureU;
layout(set = 1, binding = 2) uniform texture2D m_TextureV;
layout(set = 1, binding = 3) uniform sampler m_Sampler;

uniform mediump mat3 yuvCoeff;

// Y - 16, Cb - 128, Cr - 128
const mediump vec3 offsets = vec3(-0.0625, -0.5, -0.5);

lowp vec4 wrappedSamplerRgb(vec2 wrappedCoord, vec4 texRect, float lodBias) 
{
    if (g_WrapModeS == 2 && (wrappedCoord.x < texRect[0] || wrappedCoord.x > texRect[2]) ||
        g_WrapModeT == 2 && (wrappedCoord.y < texRect[1] || wrappedCoord.y > texRect[3]))
        return vec4(0.0);

    lowp float y = texture(sampler2D(m_TextureY, m_Sampler), wrappedCoord, lodBias).r;
    lowp float u = texture(sampler2D(m_TextureU, m_Sampler), wrappedCoord, lodBias).r;
    lowp float v = texture(sampler2D(m_TextureV, m_Sampler), wrappedCoord, lodBias).r;
    return vec4(yuvCoeff * (vec3(y, u, v) + offsets), 1.0);
}
