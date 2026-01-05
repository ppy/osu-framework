#ifndef PATH_PREPASS_VS
#define PATH_PREPASS_VS

#include "sh_Utils.h"

layout(location = 0) in highp vec2 m_Position;
layout(location = 1) in highp vec2 m_StartPos;
layout(location = 2) in highp vec2 m_EndPos;
layout(location = 3) in highp float m_Radius;

layout(location = 0) out highp vec2 v_Position;
layout(location = 1) out highp vec2 v_StartPos;
layout(location = 2) out highp vec2 v_EndPos;
layout(location = 3) out highp float v_Radius;

void main(void)
{
    v_Position = m_Position;
    v_StartPos = m_StartPos;
    v_EndPos = m_EndPos;
    v_Radius = m_Radius;
    gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
}

#endif