#ifdef GL_ES
    precision mediump float;
#endif

varying vec2 v_MaskingPosition;
varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform sampler2D m_Sampler;
uniform float g_CornerRadius;
uniform vec4 g_MaskingRect;
uniform float g_BorderThickness;
uniform vec4 g_BorderColour;


float distanceFromRoundedRect()
{
    // Compute offset distance from masking rect in masking space.
    vec2 topLeftOffset = g_MaskingRect.xy - v_MaskingPosition;
    vec2 bottomRightOffset = v_MaskingPosition - g_MaskingRect.zw;

    vec2 distanceFromShrunkRect = max(bottomRightOffset + vec2(g_CornerRadius), topLeftOffset + vec2(g_CornerRadius));
    float maxDist = max(distanceFromShrunkRect.x, distanceFromShrunkRect.y);

    if (maxDist <= 0.0)
        return maxDist;
    else
        return length(max(vec2(0.0), distanceFromShrunkRect));
}

void main(void)
{
    float dist = distanceFromRoundedRect();

    // This correction is needed to avoid fading of the alpha value for radii below 1px.
    float radiusCorrection = max(0.0, 1.0 - g_CornerRadius);
    float fadeStart = g_CornerRadius + radiusCorrection;
    float alphaFactor = min(fadeStart - dist, 1.0);
    if (alphaFactor <= 0.0)
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    float borderStart = fadeStart - g_BorderThickness + 1.0;
    float colourWeight = min(borderStart - dist, 1.0);
    if (colourWeight <= 0.0)
    {
        gl_FragColor = vec4(g_BorderColour.r, g_BorderColour.g, g_BorderColour.b, g_BorderColour.a * alphaFactor);
        return;
    }

    gl_FragColor =
        colourWeight * vec4(v_Colour.r, v_Colour.g, v_Colour.b, v_Colour.a * alphaFactor) * texture2D(m_Sampler, v_TexCoord, -0.9) +
        (1.0 - colourWeight) * g_BorderColour;
}
