#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

uniform lowp sampler2D m_Sampler;

void main(void) 
{
    gl_FragColor = getRoundedColor(toSRGB(texture2D(m_Sampler, wrap(v_TexCoord, v_TexRect), -0.9)));
}
