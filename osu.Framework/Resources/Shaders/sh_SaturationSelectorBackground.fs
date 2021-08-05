#include "sh_Utils.h"

varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;

uniform mediump float hue;

void main(void)
{
    vec2 resolution = vec2(v_TexRect[2] - v_TexRect[0], v_TexRect[3] - v_TexRect[1]);
    vec2 pixelPos = v_TexCoord / resolution;
    gl_FragColor = hsv2rgb(vec4(hue, pixelPos.x, 1.0 - pixelPos.y, 1.0));
}
