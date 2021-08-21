#include "sh_Utils.h"

varying highp vec2 v_TexCoord;
varying mediump vec4 v_TexRect;

void main(void)
{
    float hueValue = v_TexCoord.x / (v_TexRect.z - v_TexRect.x);
    gl_FragColor = hsv2rgb(vec4(hueValue, 1, 1, 1));
}
