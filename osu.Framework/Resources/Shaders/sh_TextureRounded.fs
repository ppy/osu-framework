#include "sh_Utils.h"
#include "sh_Masking.h"

uniform lowp sampler2D m_Sampler;

void main(void) 
{
    gl_FragColor = getRoundedColor(toSRGB(texture2D(m_Sampler, v_TexCoord, -0.9)));
}
