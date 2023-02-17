#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"

IN(2) highp vec2 v_TexCoord;

UNIFORM_BLOCK(0, m_HueData)
{
    mediump float hue;
};

OUT(0) vec4 o_Colour;

void main(void)
{
    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = v_TexCoord / resolution;
    o_Colour = getRoundedColor(toLinear(hsv2rgb(vec4(hue, pixelPos.x, 1.0 - pixelPos.y, 1.0))), v_TexCoord);
}
