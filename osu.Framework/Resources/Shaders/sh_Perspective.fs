#ifndef PERSPECTIVE_FS
#define PERSPECTIVE_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_PerspectiveParameters
{
    mediump float g_Strength;
};

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    highp float t = (v_TexCoord.y - v_TexRect.y) / v_TexRect.w;

    highp float inset = clamp(g_Strength, 0.0, 0.95) * (1.0 - t) * 0.5;
    highp float usableWidth = 1.0 - inset * 2.0;

    if (usableWidth <= 0.0)
    {
        o_Colour = vec4(0.0);
        return;
    }

    highp float x = (v_TexCoord.x - v_TexRect.x) / v_TexRect.z;
    highp float remappedX = (x - inset) / usableWidth;

    if (remappedX < 0.0 || remappedX > 1.0)
    {
        o_Colour = vec4(0.0);
        return;
    }

    highp vec2 sampleCoord = vec2(v_TexRect.x + remappedX * v_TexRect.z, v_TexCoord.y);
    vec2 wrappedCoord = wrap(sampleCoord, v_TexRect);

    o_Colour = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9), wrappedCoord);
}

#endif
