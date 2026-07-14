#ifndef CHECKERBOARD_FS
#define CHECKERBOARD_FS

#undef HIGH_PRECISION_VERTEX
#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = (v_TexCoord - v_TexRect.xy) / resolution;

    const int line_count = 20;
    pixelPos *= line_count;
    
    bool evenLineV = mod(int(pixelPos.x), 2.0) == 0.0;
    float modH = mod(int(pixelPos.y), 2.0);
    bool dark = evenLineV ? modH == 0.0 : modH != 0.0;

    o_Colour = getRoundedColor(vec4(vec3(dark ? 0.15 : 0.25), 1.0), v_TexCoord);
}

#endif
