#include "sh_Utils.h"

layout(location = 0) in highp vec3 m_Position;
layout(location = 1) in lowp vec4 m_Colour;
layout(location = 2) in highp vec2 m_TexCoord;

layout(location = 0) out highp vec2 v_MaskingPosition;
layout(location = 1) out lowp vec4 v_Colour;
layout(location = 2) out highp vec2 v_TexCoord;
layout(location = 3) out highp vec4 v_TexRect;
layout(location = 4) out mediump vec2 v_BlendRange;

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
