#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

uniform lowp sampler2D m_Sampler;

void main(void) 
{
    gl_FragColor = getRoundedColor(toSRGB(wrappedSampler(v_TexCoord, v_TexRect, m_Sampler, -0.9)));
}
