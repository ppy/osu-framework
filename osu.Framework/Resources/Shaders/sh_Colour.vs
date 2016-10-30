#include "sh_Utils.h"

attribute vec2 m_Position;
attribute vec4 m_Colour;

varying vec2 v_MaskingPosition;
varying vec4 v_Colour;

uniform mat4 g_ProjMatrix;
uniform mat3 g_ToMaskingSpace;

void main(void)
{
	// Transform to position to masking space.
	vec3 localPos = g_ToMaskingSpace * vec3(m_Position, 1.0);
	localPos.xy /= localPos.z;

	v_MaskingPosition = localPos.xy;
	v_Colour = m_Colour;
	gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
}
