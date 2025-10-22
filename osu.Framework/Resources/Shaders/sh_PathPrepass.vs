#ifndef PATH_PREPASS_VS
#define PATH_PREPASS_VS

#include "sh_Utils.h"

layout(location = 0) in highp vec2 m_Position;
layout(location = 1) in highp vec2 m_RelativePos;

layout(location = 0) out highp vec2 v_RelativePos;

void main(void)
{
    v_RelativePos = m_RelativePos;
    gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
}

#endif