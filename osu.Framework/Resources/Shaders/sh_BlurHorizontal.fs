#ifdef GL_ES
    precision mediump float;
#endif

#include "sh_Blur2D.h";

varying vec2 v_TexCoord;

uniform sampler2D m_Sampler;

uniform vec2 g_TexSize;
uniform int g_Radius;
uniform float g_Sigma;

void main(void)
{
	gl_FragColor = blur(m_Sampler, g_Radius, vec2(1.0, 0.0), v_TexCoord, g_TexSize, g_Sigma);
}