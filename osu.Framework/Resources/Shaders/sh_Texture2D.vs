#include "sh_Utils.h"

IN(0) highp vec2 m_Position;
IN(1) lowp vec4 m_Colour;
IN(2) highp vec2 m_TexCoord;
IN(3) highp vec4 m_TexRect;
IN(4) mediump vec2 m_BlendRange;

OUT(0) highp vec2 v_MaskingPosition;
OUT(1) lowp vec4 v_Colour;
OUT(2) highp vec2 v_TexCoord;
OUT(3) highp vec4 v_TexRect;
OUT(4) mediump vec2 v_BlendRange;

uniform highp mat4 g_ProjMatrix;
uniform highp mat3 g_ToMaskingSpace;

void main(void)
{
	// Transform from screen space to masking space.
	highp vec3 maskingPos = g_ToMaskingSpace * vec3(m_Position, 1.0);
	v_MaskingPosition = maskingPos.xy / maskingPos.z;

	v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	v_TexRect = m_TexRect;
	v_BlendRange = m_BlendRange;

	gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
}
