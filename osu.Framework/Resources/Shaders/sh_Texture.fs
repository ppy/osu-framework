#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

in mediump vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;

out vec4 o_Colour;

void main(void) 
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    o_Colour = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Sampler, -0.9), wrappedCoord);
}
