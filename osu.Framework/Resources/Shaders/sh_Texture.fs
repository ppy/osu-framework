#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(set = 0, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 0, binding = 1) uniform lowp sampler m_Sampler;
layout(std140, set = 0, binding = 2) uniform AuxData { AuxTextureData m_TextureData; };

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    o_Colour = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, m_TextureData, -0.9), wrappedCoord);
}
