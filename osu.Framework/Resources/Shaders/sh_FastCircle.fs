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
    highp vec2 pixelPos = v_TexCoord / v_TexRect.zw;
    highp vec2 pixelSize = vec2(1.5) / v_TexRect.zw;

    highp float alpha = smoothstep(0.5, 0.5 - min(max(pixelSize.x, pixelSize.y), 0.1), distance(pixelPos, vec2(0.5)));

    o_Colour = getRoundedColor(vec4(vec3(1.0), alpha), vec2(0.0));
}

#endif
