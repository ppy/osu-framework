#ifndef PATH_PREPASS_FS
#define PATH_PREPASS_FS

#include "sh_CircularProgressUtils.h"
#include "sh_Utils.h"

layout(location = 0) in highp vec2 v_Position;
layout(location = 1) in highp vec2 v_StartPos;
layout(location = 2) in highp vec2 v_EndPos;
layout(location = 3) in highp float v_Radius;

layout(location = 0) out mediump float o_Colour;

void main(void) 
{
    o_Colour = clamp(1.0 - dstToLine(v_StartPos, v_EndPos, v_Position) / v_Radius, 0.0, 1.0);
}

#endif