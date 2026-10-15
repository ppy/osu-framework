#ifndef PATH_PREPASS_FS
#define PATH_PREPASS_FS

#include "sh_CircularProgressUtils.h"
#include "sh_Utils.h"

layout(location = 0) in highp vec2 v_Position;
layout(location = 1) in highp vec2 v_StartPos;
layout(location = 2) in highp vec2 v_EndPos;

layout(std140, set = 0, binding = 0) uniform m_PathParameters
{
	mediump float radius;
};

layout(location = 0) out mediump float o_Colour;

void main(void) 
{
    o_Colour = clamp(1.0 - dstToLine(v_StartPos, v_EndPos, v_Position) / radius, 0.0, 1.0);
}

#endif