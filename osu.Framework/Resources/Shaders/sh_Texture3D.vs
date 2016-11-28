#include "sh_Utils.h"

attribute vec3 m_Position;
attribute vec4 m_Colour;
attribute vec2 m_TexCoord;

varying vec2 v_DrawingPosition;
varying vec2 v_MaskingPosition;
varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform mat4 g_ProjMatrix;
uniform mat3 g_ToMaskingSpace;
uniform mat3 g_ToDrawingSpace;

void main(void)
{
	// Transform to position to masking space.
	vec3 maskingPos = g_ToMaskingSpace * vec3(m_Position.xy, 1.0);
	v_MaskingPosition = maskingPos.xy / maskingPos.z;

	vec3 drawingPos = g_ToDrawingSpace * vec3(m_Position.xy, 1.0);
	v_DrawingPosition = drawingPos.xy / drawingPos.z;

	v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	gl_Position = g_ProjMatrix * vec4(m_Position, 1.0);
}