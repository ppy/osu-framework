#ifndef FAST_CIRCLE_FS
#define FAST_CIRCLE_FS

#undef HIGH_PRECISION_VERTEX
#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"

layout(location = 2) in highp vec2 v_TexCoord;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = v_TexCoord / resolution;

    o_Colour = getRoundedColor(distance(pixelPos, vec2(0.5)) < 0.5 ? vec4(1.0) : vec4(vec3(1.0), 0.0), v_TexCoord);
}

#endif
