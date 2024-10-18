#ifndef TEXTURE_MASK_FS
#define TEXTURE_MASK_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_MaskParameters
{
	mediump float g_MaskCutoff;
};

layout(set = 1, binding = 0) uniform highp texture2D m_Texture;
layout(set = 1, binding = 1) uniform highp sampler m_Sampler;

layout(set = 2, binding = 0) uniform lowp texture2D m_Mask;
layout(set = 2, binding = 1) uniform lowp sampler m_MaskSampler;



layout(location = 0) out vec4 o_Colour;

void main(void)
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);

    vec4 mask = wrappedSampler(wrappedCoord, v_TexRect, m_Mask, m_MaskSampler, -0.9);

    if (mask.a <= g_MaskCutoff)
        discard;

    o_Colour = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9), wrappedCoord);
}

#endif
