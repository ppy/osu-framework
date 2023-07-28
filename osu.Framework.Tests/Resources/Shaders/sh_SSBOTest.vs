#ifndef SSBO_TEST_VS
#define SSBO_TEST_VS

layout(location = 0) in highp vec2 m_Position;
layout(location = 1) in int m_ColourIndex;

layout(location = 0) flat out int v_ColourIndex;

void main(void)
{
    v_ColourIndex = m_ColourIndex;
    gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
}

#endif