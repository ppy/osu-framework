#include "sh_Utils.h"

attribute vec2 m_Position;
attribute vec4 m_Colour;
attribute vec2 m_TexCoord;
attribute vec4 m_TexRect;
attribute vec2 m_BlendRange;

varying vec2 v_MaskingPosition;
varying vec4 v_Colour;
varying vec2 v_TexCoord;
varying vec4 v_TexRect;
varying vec2 v_BlendRange;

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