#ifdef GL_ES
    precision mediump float;
#endif

varying vec2 v_MaskingPosition;
varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform sampler2D m_Sampler;

uniform float g_CornerRadius;

uniform vec4 g_MaskingRect;

vec2 max3(vec2 a, vec2 b, vec2 c)
{
    return max(max(a, b), c);
}

float distanceFromRoundedRect()
{
	// Compute offset distance from masking rect in masking space.
    vec2 topLeftOffset = g_MaskingRect.xy - v_MaskingPosition;
    vec2 bottomRightOffset = v_MaskingPosition - g_MaskingRect.zw;
    vec2 distanceFromShrunkRect = max3(vec2(0.0), bottomRightOffset + vec2(g_CornerRadius), topLeftOffset + vec2(g_CornerRadius));
    return length(distanceFromShrunkRect);
}

void main(void)
{
    float dist = g_CornerRadius == 0.0 ? 0.0 : distanceFromRoundedRect();
    gl_FragColor = v_Colour * texture2D(m_Sampler, v_TexCoord, -0.9);

    // This correction is needed to avoid fading of the alpha value for radii below 1px.
    float radiusCorrection = max(0.0, 1.0 - g_CornerRadius);
    if (dist > g_CornerRadius - 1.0 + radiusCorrection)
        gl_FragColor.a *= max(0.0, g_CornerRadius - dist + radiusCorrection);
}
