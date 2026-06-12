#ifndef PATH_PREPASS_FS
#define PATH_PREPASS_FS

#include "sh_CircularProgressUtils.h"

layout(location = 0) in highp vec2 v_Position;
layout(location = 1) in highp vec2 v_StartPos;
layout(location = 2) in highp vec2 v_EndPos;
layout(location = 3) in highp float v_Radius;

layout(location = 0) out vec4 o_Colour;

void main(void) 
{    
    highp float dst = clamp(dstToLine(v_StartPos, v_EndPos, v_Position), 0.0, v_Radius);
    highp float result = 1.0 - dst / v_Radius; // [0, 1] 24 bit

    // encode 24-bit float as 3 8-bit floats
    highp float v = result * 16777215.0;

    highp float r = floor(v / 65536.0) / 255.0;
    highp float g = floor(mod(v, 65536.0) / 256.0) / 255.0;
    highp float b = mod(v, 256.0) / 255.0;

    o_Colour = vec4(r, g, b, float(dst < v_Radius));
}

#endif