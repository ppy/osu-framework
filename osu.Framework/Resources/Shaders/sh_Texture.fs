#ifdef GL_ES
    precision mediump float;
#endif

#include "sh_Utils.h"

varying vec2 v_DrawingPosition;
varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform vec4 g_DrawingRect;
uniform float g_DrawingBlendRange;

uniform sampler2D m_Sampler;

float distanceFromDrawingRect()
{
	vec2 topLeftOffset = g_DrawingRect.xy - v_DrawingPosition;
    vec2 bottomRightOffset = v_DrawingPosition - g_DrawingRect.zw;
	vec2 xyDistance = max(topLeftOffset, bottomRightOffset);
	return max(xyDistance.x, xyDistance.y);
}

void main(void)
{
	gl_FragColor = toSRGB(v_Colour * texture2D(m_Sampler, v_TexCoord, -0.9));

	if (g_DrawingBlendRange > 0.0)
		gl_FragColor.a *= clamp(1.0 - distanceFromDrawingRect() / g_DrawingBlendRange, 0.0, 1.0);
}