#include "sh_TextureWrapping.h"

uniform sampler2D m_SamplerY;
uniform sampler2D m_SamplerU;
uniform sampler2D m_SamplerV;

uniform mediump mat3 yuvCoeff;

// Y - 16, Cb - 128, Cr - 128
const mediump vec3 offsets = vec3(-0.0625, -0.5, -0.5);

lowp vec4 wrappedSamplerRgb(vec2 wrappedCoord, vec4 texRect, float lodBias) 
{
    if (g_WrapModeS == 2 && (wrappedCoord.x < texRect[0] || wrappedCoord.x > texRect[2]) ||
        g_WrapModeT == 2 && (wrappedCoord.y < texRect[1] || wrappedCoord.y > texRect[3]))
        return vec4(0.0);

    lowp float y = texture2D(m_SamplerY, wrappedCoord, lodBias).r;
    lowp float u = texture2D(m_SamplerU, wrappedCoord, lodBias).r;
    lowp float v = texture2D(m_SamplerV, wrappedCoord, lodBias).r;
    return vec4(yuvCoeff * (vec3(y, u, v) + offsets), 1.0);
}
