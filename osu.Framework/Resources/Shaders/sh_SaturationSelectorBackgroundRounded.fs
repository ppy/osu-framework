#include "sh_Utils.h"
#include "sh_Masking.h"

varying highp vec2 v_TexCoord;

uniform mediump float hue;

void main(void)
{
    vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    vec2 pixelPos = v_TexCoord / resolution;
    gl_FragColor = getRoundedColor(hsv2rgb(vec4(hue, pixelPos.x, 1.0 - pixelPos.y, 1.0)), v_TexCoord);
}
