#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_yuv2rgb.h"

void main(void) 
{
    gl_FragColor = getRoundedColor(vec4(sampleRgb(v_TexCoord), 1.0));
}