#include "sh_Utils.h"

IN_VAR(0) highp vec2 m_Position;
IN_VAR(1) lowp vec4 m_Colour;
IN_VAR(2) highp vec2 m_TexCoord;
IN_VAR(3) highp vec4 m_TexRect;
IN_VAR(4) mediump vec2 m_BlendRange;

OUT_VAR(0) highp vec2 v_MaskingPosition;
OUT_VAR(1) lowp vec4 v_Colour;
OUT_VAR(2) highp vec2 v_TexCoord;
OUT_VAR(3) highp vec4 v_TexRect;
OUT_VAR(4) mediump vec2 v_BlendRange;

void main(void)
{
	// Transform from screen space to masking space.
	highp vec3 maskingPos = g_ToMaskingSpace * vec3(m_Position, 1.0);
	v_MaskingPosition = maskingPos.xy / maskingPos.z;

	v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	v_TexRect = m_TexRect;
	v_BlendRange = m_BlendRange;

	gl_Position = g_ProjMatrix * vec4(m_Position, 0.0, 1.0);
}
