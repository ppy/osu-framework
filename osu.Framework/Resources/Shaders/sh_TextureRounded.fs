#ifdef GL_ES
	precision mediump float;
#endif

#include "sh_Utils.h"

varying vec2 v_MaskingPosition;
varying vec4 v_Colour;
varying vec2 v_TexCoord;
varying vec4 v_TexRect;
varying vec2 v_BlendRange;

uniform sampler2D m_Sampler;
uniform float g_CornerRadius;
uniform vec4 g_MaskingRect;
uniform float g_BorderThickness;
uniform vec4 g_BorderColour;

uniform float g_MaskingBlendRange;

uniform float g_AlphaExponent;

uniform vec2 g_EdgeOffset;

uniform bool g_DiscardInner;
uniform float g_InnerCornerRadius;

float distanceFromRoundedRect(vec2 offset, float radius)
{
	vec2 maskingPosition = v_MaskingPosition + offset;

	// Compute offset distance from masking rect in masking space.
	vec2 topLeftOffset = g_MaskingRect.xy - maskingPosition;
	vec2 bottomRightOffset = maskingPosition - g_MaskingRect.zw;

	vec2 distanceFromShrunkRect = max(
		bottomRightOffset + vec2(radius),
		topLeftOffset + vec2(radius));

	float maxDist = max(distanceFromShrunkRect.x, distanceFromShrunkRect.y);

	// Inside the shrunk rectangle
	if (maxDist <= 0.0)
		return maxDist;
	// Outside of the shrunk rectangle
	else
		return length(max(vec2(0.0), distanceFromShrunkRect));
}

float distanceFromDrawingRect()
{
	vec2 topLeftOffset = v_TexRect.xy - v_TexCoord;
	topLeftOffset = vec2(
		v_BlendRange.x > 0.0 ? topLeftOffset.x / v_BlendRange.x : 0.0,
		v_BlendRange.y > 0.0 ? topLeftOffset.y / v_BlendRange.y : 0.0);

	vec2 bottomRightOffset = v_TexCoord - v_TexRect.zw;
	bottomRightOffset = vec2(
		v_BlendRange.x > 0.0 ? bottomRightOffset.x / v_BlendRange.x : 0.0,
		v_BlendRange.y > 0.0 ? bottomRightOffset.y / v_BlendRange.y : 0.0);

	vec2 xyDistance = max(topLeftOffset, bottomRightOffset);
	return max(xyDistance.x, xyDistance.y);
}

void main(void)
{
	float dist = distanceFromRoundedRect(vec2(0.0), g_CornerRadius);
	float alphaFactor = 1.0;
	vec4 texel = texture2D(m_Sampler, v_TexCoord, -0.9);

	// Discard inner pixels
	if (g_DiscardInner)
	{
		float innerDist = (g_EdgeOffset == vec2(0.0) && g_InnerCornerRadius == g_CornerRadius) ?
			dist : distanceFromRoundedRect(g_EdgeOffset, g_InnerCornerRadius);

		// v_BlendRange is set from outside in a hacky way to tell us the g_MaskingBlendRange used for the rounded
		// corners of the edge effect container itself. We can then derive the alpha factor for smooth inner edge
		// effect from that.
		float innerBlendFactor = (g_InnerCornerRadius - g_MaskingBlendRange - innerDist) / v_BlendRange.x;
		if (innerBlendFactor > 1.0)
		{
			gl_FragColor = vec4(0.0);
			return;
		}

		// We exponentiate our factor to exactly counteract the later exponentiation by g_AlphaExponent for a smoother inner border.
		alphaFactor = pow(min(1.0 - innerBlendFactor, 1.0), 1.0 / g_AlphaExponent);
	}

	dist /= g_MaskingBlendRange;

	// This correction is needed to avoid fading of the alpha value for radii below 1px.
	float radiusCorrection = g_CornerRadius <= 0.0 ? g_MaskingBlendRange : max(0.0, g_MaskingBlendRange - g_CornerRadius);
	float fadeStart = (g_CornerRadius + radiusCorrection) / g_MaskingBlendRange;
	alphaFactor *= min(fadeStart - dist, 1.0);

	if (v_BlendRange.x > 0.0 || v_BlendRange.y > 0.0)
		alphaFactor *= clamp(1.0 - distanceFromDrawingRect(), 0.0, 1.0);

	if (alphaFactor <= 0.0)
	{
		gl_FragColor = vec4(0.0);
		return;
	}

	// This ends up softening glow without negatively affecting edge smoothness much.
	alphaFactor = pow(alphaFactor, g_AlphaExponent);

	float borderStart = 1.0 + fadeStart - g_BorderThickness;
	float colourWeight = min(borderStart - dist, 1.0);
	if (colourWeight <= 0.0)
	{
		gl_FragColor = toSRGB(vec4(g_BorderColour.rgb, g_BorderColour.a * alphaFactor));
		return;
	}

	gl_FragColor = toSRGB(
		colourWeight * vec4(v_Colour.rgb, v_Colour.a * alphaFactor) * texel +
		(1.0 - colourWeight) * g_BorderColour);
}
