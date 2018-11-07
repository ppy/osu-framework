#version 130
#include "sh_Utils.h"

in vec2 m_Position;
in vec4 m_Colour;
in vec2 m_TexCoord;
in vec4 m_TexRect;
in vec2 m_BlendRange;

out vec2 v_MaskingPosition;
out vec4 v_Colour;
out vec2 v_TexCoord;
out vec4 v_TexRect;
out vec2 v_BlendRange;

uniform mat4 g_ProjMatrix;
uniform mat3 g_ToMaskingSpace;

void main(void)
{
	// Transform to position to masking space.
	vec3 maskingPos = g_ToMaskingSpace * vec3(m_Position, 1.0);
	v_MaskingPosition = maskingPos.xy / maskingPos.z;

	v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	v_TexRect = m_TexRect;
	v_BlendRange = m_BlendRange;
	gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
}