#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

varying mediump vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;

void main(void) 
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    gl_FragColor = getRoundedColor(toSRGB(wrappedSampler(wrappedCoord, v_TexRect, m_Sampler, -0.9)), wrappedCoord);
}
