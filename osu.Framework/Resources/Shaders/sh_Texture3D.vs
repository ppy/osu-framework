#include "sh_Utils.h"

in highp vec3 m_Position;
in lowp vec4 m_Colour;
in highp vec2 m_TexCoord;

out highp vec2 v_MaskingPosition;
out lowp vec4 v_Colour;
out highp vec2 v_TexCoord;
out highp vec4 v_TexRect;
out mediump vec2 v_BlendRange;

uniform mat4 g_ProjMatrix;
uniform mat3 g_ToMaskingSpace;

void main(void)
{
	// Transform to position to masking space.
	vec3 maskingPos = g_ToMaskingSpace * vec3(m_Position.xy, 1.0);
	v_MaskingPosition = maskingPos.xy / maskingPos.z;

	v_TexRect = vec4(0.0);
	v_BlendRange = vec2(0.0);

	v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	gl_Position = g_ProjMatrix * vec4(m_Position, 1.0);
}
