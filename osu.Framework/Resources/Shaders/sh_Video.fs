#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_yuv2rgb.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(location = 0) out vec4 o_Colour;

void main(void) 
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    o_Colour = getRoundedColor(toLinear(wrappedSamplerRgb(wrappedCoord, v_TexRect, 0.0)), wrappedCoord);
}