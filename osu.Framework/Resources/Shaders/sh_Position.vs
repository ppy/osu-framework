IN(0) highp vec2 m_Position;

OUT(0) highp vec2 v_Position;

void main(void)
{
	gl_Position = g_ProjMatrix * vec4(m_Position.xy, 1.0, 1.0);
	v_Position = m_Position;
}