#include "sh_Utils.h"
#include "sh_yuv2rgb.h"

varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;

void main() {
  gl_FragColor = toSRGB(v_Colour) * vec4(sampleRgb(v_TexCoord), 1.0);
}