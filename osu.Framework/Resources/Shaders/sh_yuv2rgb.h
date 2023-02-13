#include "sh_TextureWrapping.h"

UNIFORM_TEXTURE(0, m_TextureY, m_SamplerY);
UNIFORM_TEXTURE(1, m_TextureU, m_SamplerU);
UNIFORM_TEXTURE(2, m_TextureV, m_SamplerV);

UNIFORM_BLOCK(3, m_yuvData)
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

    lowp float y = SampleTexture(m_TextureY, m_SamplerY, wrappedCoord, lodBias).r;
    lowp float u = SampleTexture(m_TextureU, m_SamplerU, wrappedCoord, lodBias).r;
    lowp float v = SampleTexture(m_TextureV, m_SamplerV, wrappedCoord, lodBias).r;
    return vec4(yuvCoeff * (vec3(y, u, v) + offsets), 1.0);
}
