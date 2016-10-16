attribute vec3 m_Position;
attribute vec4 m_Colour;
attribute vec2 m_TexCoord;

varying vec2 v_Position;
varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform mat4 g_ProjMatrix;

void main(void)
{
	v_Position = m_Position.xy;
	v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	gl_Position = g_ProjMatrix * vec4(m_Position, 1.0);
}