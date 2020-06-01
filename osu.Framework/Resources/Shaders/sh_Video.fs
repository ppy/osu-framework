#include "sh_Utils.h"
#include "sh_yuv2rgb.h"

varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;

void main() {
    gl_FragColor = toSRGB(v_Colour) * wrappedSamplerRgb(wrap(v_TexCoord, v_TexRect), v_TexRect, 0.0);
}