#include "sh_Utils.h"

IN(0) highp vec3 m_Position;
IN(1) lowp vec4 m_Colour;
IN(2) highp vec2 m_TexCoord;

OUT(0) highp vec2 v_MaskingPosition;
OUT(1) lowp vec4 v_Colour;
OUT(2) highp vec2 v_TexCoord;
OUT(3) highp vec4 v_TexRect;
OUT(4) mediump vec2 v_BlendRange;

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
