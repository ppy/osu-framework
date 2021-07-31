#include "sh_Utils.h"

varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;

uniform mediump float hueValue;

void main(void)
{
    vec2 resolution = vec2(v_TexRect[2] - v_TexRect[0], v_TexRect[3] - v_TexRect[0]);
    vec2 pixelPos = vec2(v_TexCoord.x/resolution.x, v_TexCoord.y/resolution.y);
    gl_FragColor = hsv2rgb(vec4(hueValue, pixelPos.x, 1.0 - pixelPos.y, 1.0));
}
