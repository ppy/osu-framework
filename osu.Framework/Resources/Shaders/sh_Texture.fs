#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

IN_VAR(2) mediump vec2 v_TexCoord;

UNIFORM_TEXTURE(0, m_Texture, m_Sampler);

OUT_VAR(0) vec4 o_Colour;

void main(void) 
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    o_Colour = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9), wrappedCoord);
}
