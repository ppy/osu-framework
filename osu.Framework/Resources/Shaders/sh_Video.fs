#include "sh_Utils.h"
#include "sh_yuv2rgb.h"

varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;

void main() {
  gl_FragColor = vec4(sampleRgb(v_TexCoord), v_Colour.a);
}