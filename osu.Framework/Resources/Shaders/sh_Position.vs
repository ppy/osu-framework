#version 130
in vec2 m_Position;

out vec2 v_Position;

uniform mat4 g_ProjMatrix;

void main(void)
{
	gl_Position = g_ProjMatrix * vec4(m_Position.xy, 1.0, 1.0);
	v_Position = m_Position;
}