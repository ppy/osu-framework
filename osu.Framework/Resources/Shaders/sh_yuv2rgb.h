#include "sh_TextureWrapping.h"

layout(set = 0, binding = 0) uniform lowp texture2D m_TextureY;
layout(set = 0, binding = 1) uniform lowp sampler m_SamplerY;
layout(set = 1, binding = 0) uniform lowp texture2D m_TextureU;
layout(set = 1, binding = 1) uniform lowp sampler m_SamplerU;
layout(set = 2, binding = 0) uniform lowp texture2D m_TextureV;
layout(set = 2, binding = 1) uniform lowp sampler m_SamplerV;

layout(std140, set = 3, binding = 0) uniform m_yuvData
{
    mediump mat3 yuvCoeff;
};

// Y - 16, Cb - 128, Cr - 128
const mediump vec3 offsets = vec3(-0.0625, -0.5, -0.5);

lowp vec4 wrappedSamplerRgb(vec2 wrappedCoord, vec4 texRect, float lodBias) 
{
    if (g_WrapModeS == 2 && (wrappedCoord.x < texRect[0] || wrappedCoord.x > texRect[2]) ||
        g_WrapModeT == 2 && (wrappedCoord.y < texRect[1] || wrappedCoord.y > texRect[3]))
        return vec4(0.0);

    lowp float y = texture(sampler2D(m_TextureY, m_SamplerY), wrappedCoord, lodBias).r;
    lowp float u = texture(sampler2D(m_TextureU, m_SamplerU), wrappedCoord, lodBias).r;
    lowp float v = texture(sampler2D(m_TextureV, m_SamplerV), wrappedCoord, lodBias).r;
    return vec4(yuvCoeff * (vec3(y, u, v) + offsets), 1.0);
}
