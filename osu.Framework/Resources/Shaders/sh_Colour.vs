#version 130
#include "sh_Utils.h"

in vec2 m_Position;
in vec4 m_Colour;

out vec2 v_MaskingPosition;
out vec4 v_Colour;

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
