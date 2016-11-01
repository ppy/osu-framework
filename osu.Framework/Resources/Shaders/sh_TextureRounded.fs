#ifdef GL_ES
    precision mediump float;
#endif

#include "sh_Utils.h"

varying vec2 v_MaskingPosition;
varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform sampler2D m_Sampler;
uniform float g_CornerRadius;
uniform vec4 g_MaskingRect;
uniform float g_BorderThickness;
uniform vec4 g_BorderColour;

uniform float g_LinearBlendRange;

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
    float radiusCorrection = g_CornerRadius <= 0.0 ? 1.0 : max(0.0, g_LinearBlendRange - g_CornerRadius);
    float fadeStart = g_CornerRadius + radiusCorrection;
    float alphaFactor = min((fadeStart - dist) / g_LinearBlendRange, 1.0);
    if (alphaFactor <= 0.0)
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    float borderStart = fadeStart - g_BorderThickness + g_LinearBlendRange;
    float colourWeight = min((borderStart - dist) / g_LinearBlendRange, 1.0);
    if (colourWeight <= 0.0)
    {
        gl_FragColor = toSRGB(vec4(g_BorderColour.rgb, g_BorderColour.a * alphaFactor));
		gl_FragColor = vec4(gl_FragColor.rgb * gl_FragColor.a, gl_FragColor.a);
        return;
    }

    gl_FragColor = toSRGB(
		colourWeight * vec4(v_Colour.rgb, v_Colour.a * alphaFactor) * texture2D(m_Sampler, v_TexCoord, -0.9) +
        (1.0 - colourWeight) * g_BorderColour);
		
	gl_FragColor = vec4(gl_FragColor.rgb * gl_FragColor.a, gl_FragColor.a);
}
