#ifdef GL_ES
    precision mediump float;
#endif

varying vec2 v_MaskingPosition;
varying vec4 v_Colour;

uniform float g_CornerRadius;
uniform vec4 g_MaskingRect;
uniform float g_BorderThickness;
uniform vec4 g_BorderColour;

uniform float g_PixelScale;

float distanceFromRoundedRect()
{
    // Compute offset distance from masking rect in masking space.
    vec2 topLeftOffset = g_MaskingRect.xy - v_MaskingPosition;
    vec2 bottomRightOffset = v_MaskingPosition - g_MaskingRect.zw;

    vec2 distanceFromShrunkRect = max(
        bottomRightOffset + vec2(g_CornerRadius),
        topLeftOffset + vec2(g_CornerRadius));

    float maxDist = max(distanceFromShrunkRect.x, distanceFromShrunkRect.y);

    // Inside the shrunk rectangle
    if (maxDist <= 0.0)
        return maxDist;
    // Outside of the shrunk rectangle
    else
        return length(max(vec2(0.0), distanceFromShrunkRect));
}

void main(void)
{
    float dist = distanceFromRoundedRect();

    // This correction is needed to avoid fading of the alpha value for radii below 1px.
    float radiusCorrection = g_CornerRadius <= 0.0 ? 1.0 : max(0.0, g_PixelScale - g_CornerRadius);
    float fadeStart = g_CornerRadius + radiusCorrection;
    float alphaFactor = min((fadeStart - dist) / g_PixelScale, 1.0);
    if (alphaFactor <= 0.0)
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    float borderStart = fadeStart - g_BorderThickness + g_PixelScale;
    float colourWeight = min((borderStart - dist) / g_PixelScale, 1.0);
    if (colourWeight <= 0.0)
    {
        gl_FragColor = vec4(g_BorderColour.rgb, g_BorderColour.a * alphaFactor);
        return;
    }

    gl_FragColor =
        colourWeight * vec4(v_Colour.rgb, v_Colour.a * alphaFactor) +
        (1.0 - colourWeight) * g_BorderColour;
}
