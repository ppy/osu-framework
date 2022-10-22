#include "sh_Utils.h"

varying highp vec2 v_TexCoord;
varying highp vec4 v_TexRect;

uniform mediump float hue;

void main(void)
{
    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = v_TexCoord / resolution;
    gl_FragColor = hsv2rgb(vec4(hue, pixelPos.x, 1.0 - pixelPos.y, 1.0));
}
