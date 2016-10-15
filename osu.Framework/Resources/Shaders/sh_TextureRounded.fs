#ifdef GL_ES
    precision mediump float;
#endif

varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform sampler2D m_Sampler;

uniform float g_Radius;
uniform vec4 g_TexRect;
uniform vec2 g_TexSize;

vec2 max3(vec2 a, vec2 b, vec2 c)
{
	return max(max(a, b), c);
}

float distanceFromRoundedRect()
{
	vec2 topLeftOffset = g_TexSize * (g_TexRect.xy - v_TexCoord);
	vec2 bottomRightOffset = g_TexSize * (v_TexCoord - g_TexRect.zw);

	vec2 distanceFromShrunkRect = max3(vec2(0.0), bottomRightOffset + g_Radius, topLeftOffset + g_Radius);
	return length(distanceFromShrunkRect);
}

void main(void)
{
	float dist = distanceFromRoundedRect();
	gl_FragColor = v_Colour * texture2D(m_Sampler, v_TexCoord, -0.9);
	if (dist > g_Radius-1.0)
		gl_FragColor.a *= max(0.0, g_Radius - dist);
}
