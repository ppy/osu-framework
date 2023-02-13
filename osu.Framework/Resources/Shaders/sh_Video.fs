#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_yuv2rgb.h"

IN_VAR(2) mediump vec2 v_TexCoord;

OUT_VAR(0) vec4 o_Colour;

void main(void) 
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    o_Colour = getRoundedColor(toLinear(wrappedSamplerRgb(wrappedCoord, v_TexRect, 0.0)), wrappedCoord);
}