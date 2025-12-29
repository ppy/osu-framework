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
    o_Colour = vec4(vec3(1.0 - dst / v_Radius), float(dst < v_Radius));
}

#endif