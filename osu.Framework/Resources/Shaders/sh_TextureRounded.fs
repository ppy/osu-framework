#ifdef GL_ES
    precision mediump float;
#endif

varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform sampler2D m_Sampler;

uniform vec2 g_Radius;
uniform vec4 g_TexRect;

vec2 max3(vec2 a, vec2 b, vec2 c)
{
	return max(max(a, b), c);
}

bool isInside()
{
	vec2 topLeftOffset = g_TexRect.xy - v_TexCoord;
	vec2 bottomRightOffset = v_TexCoord - g_TexRect.zw;

	vec2 distanceFromShrunkRect = max3(vec2(0.0), bottomRightOffset / g_Radius + 1.0, topLeftOffset / g_Radius + 1.0);
	return length(distanceFromShrunkRect) < 1.0;
}

void main(void)
{
	gl_FragColor = isInside() ? (v_Colour * texture2D(m_Sampler, v_TexCoord, -0.9)) : vec4(0.0);
}
