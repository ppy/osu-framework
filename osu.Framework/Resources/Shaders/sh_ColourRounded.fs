#ifdef GL_ES
    precision mediump float;
#endif

varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform vec2 g_Radius;
uniform vec4 g_TexRect;

vec2 max3(vec2 a, vec2 b, vec2 c)
{
	return max(max(a, b), c);
}

float distanceSqFromRoundedRect()
{
	vec2 topLeftOffset = g_TexRect.xy - v_TexCoord;
	vec2 bottomRightOffset = v_TexCoord - g_TexRect.zw;

	vec2 distanceFromShrunkRect = max3(vec2(0.0), bottomRightOffset / g_Radius + 1.0, topLeftOffset / g_Radius + 1.0);
	return dot(distanceFromShrunkRect, distanceFromShrunkRect);
}

void main(void)
{
	float distSq = distanceSqFromRoundedRect();
	if (distSq < 1.0)
		gl_FragColor = v_Colour;
	else
		gl_FragColor = vec4(0.0);
}
