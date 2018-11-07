#version 130
#ifdef GL_ES
	precision mediump float;
#endif

#include "sh_Utils.h"

in vec4 v_Colour;
in vec2 v_TexCoord;

uniform sampler2D m_Sampler;

void main(void)
{
	gl_FragColor = toSRGB(v_Colour * texture2D(m_Sampler, v_TexCoord, -0.9));
}
