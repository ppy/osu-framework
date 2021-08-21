#include "sh_Utils.h"
#include "sh_Masking.h"

varying highp vec2 v_TexCoord;

void main(void)
{
    float hueValue = v_TexCoord.x / (v_TexRect.z - v_TexRect.x);
    gl_FragColor = getRoundedColor(hsv2rgb(vec4(hueValue, 1, 1, 1)), v_TexCoord);
}
