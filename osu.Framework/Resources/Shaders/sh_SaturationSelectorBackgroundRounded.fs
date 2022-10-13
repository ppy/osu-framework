#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"

varying highp vec2 v_TexCoord;

uniform mediump float hue;

void main(void)
{
    highp vec2 resolution = vec2(v_TexRect[2] - v_TexRect[0], v_TexRect[3] - v_TexRect[1]);
    highp vec2 pixelPos = v_TexCoord / resolution;
    gl_FragColor = getRoundedColor(hsv2rgb(vec4(hue, pixelPos.x, 1.0 - pixelPos.y, 1.0)), v_TexCoord);
}
