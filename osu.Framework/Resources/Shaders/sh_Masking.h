#ifndef MASKING_H
#define MASKING_H

#include "Internal/sh_MaskingInfo.h"

layout(location = 0) in highp vec2 v_MaskingPosition;
layout(location = 1) in lowp vec4 v_Colour;

#ifdef HIGH_PRECISION_VERTEX
	layout(location = 3) in highp vec4 v_TexRect;
#else
	layout(location = 3) in mediump vec4 v_TexRect;
#endif

layout(location = 4) in mediump vec2 v_BlendRange;
layout(location = 5) flat in int v_MaskingIndex;
layout(location = 6) in highp vec2 v_ScissorPosition;

/// Positive if outside the rect, negative if inside the rect.
highp float distanceFromScissorRect()
{
	highp vec2 topLeftOffset = g_MaskingInfo.ScissorRect.xy - v_ScissorPosition;
	highp vec2 bottomRightOffset = v_ScissorPosition - g_MaskingInfo.ScissorRect.zw;

	highp vec2 distanceFromShrunkRect = max(bottomRightOffset, topLeftOffset);

	return max(distanceFromShrunkRect.x, distanceFromShrunkRect.y);
}

highp float distanceFromRoundedRect(highp vec2 offset, highp float radius)
{
	highp vec2 maskingPosition = v_MaskingPosition + offset;

	// Compute offset distance from masking rect in masking space.
	highp vec2 topLeftOffset = g_MaskingInfo.MaskingRect.xy - maskingPosition;
	highp vec2 bottomRightOffset = maskingPosition - g_MaskingInfo.MaskingRect.zw;

	highp vec2 distanceFromShrunkRect = max(
		bottomRightOffset + vec2(radius),
		topLeftOffset + vec2(radius));

	highp float maxDist = max(distanceFromShrunkRect.x, distanceFromShrunkRect.y);

	// Inside the shrunk rectangle
	if (maxDist <= 0.0)
		return maxDist;
	// Outside of the shrunk rectangle
	else
	{
		distanceFromShrunkRect = max(vec2(0.0), distanceFromShrunkRect);
		return pow(pow(distanceFromShrunkRect.x, g_MaskingInfo.CornerExponent) + pow(distanceFromShrunkRect.y, g_MaskingInfo.CornerExponent), 1.0 / g_MaskingInfo.CornerExponent);
	}
}

highp float distanceFromDrawingRect(mediump vec2 texCoord)
{
	highp vec2 topLeftOffset = v_TexRect.xy - texCoord;
	topLeftOffset = vec2(
		v_BlendRange.x > 0.0 ? topLeftOffset.x / v_BlendRange.x : 0.0,
		v_BlendRange.y > 0.0 ? topLeftOffset.y / v_BlendRange.y : 0.0);

	highp vec2 bottomRightOffset = texCoord - v_TexRect.zw;
	bottomRightOffset = vec2(
		v_BlendRange.x > 0.0 ? bottomRightOffset.x / v_BlendRange.x : 0.0,
		v_BlendRange.y > 0.0 ? bottomRightOffset.y / v_BlendRange.y : 0.0);

	highp vec2 xyDistance = max(topLeftOffset, bottomRightOffset);
	return max(xyDistance.x, xyDistance.y);
}

lowp vec4 getBorderColour()
{
    highp vec2 relativeTexCoord = v_MaskingPosition / (g_MaskingInfo.MaskingRect.zw - g_MaskingInfo.MaskingRect.xy);
    lowp vec4 top = mix(g_MaskingInfo.BorderColour[0], g_MaskingInfo.BorderColour[2], relativeTexCoord.x);
    lowp vec4 bottom = mix(g_MaskingInfo.BorderColour[1], g_MaskingInfo.BorderColour[3], relativeTexCoord.x);
    return mix(top, bottom, relativeTexCoord.y);
}

lowp vec4 getRoundedColor(lowp vec4 texel, mediump vec2 texCoord)
{
	InitMasking(v_MaskingIndex);

	if (!g_MaskingInfo.IsMasking && v_BlendRange == vec2(0.0))
	{
		return v_Colour * texel;
	}

	if (distanceFromScissorRect() > 0)
	{
		discard;
	}

	highp float dist = distanceFromRoundedRect(vec2(0.0), g_MaskingInfo.CornerRadius);
	lowp float alphaFactor = 1.0;

	// Discard inner pixels
	if (g_MaskingInfo.DiscardInner)
	{
		highp float innerDist = (g_MaskingInfo.EdgeOffset == vec2(0.0) && g_MaskingInfo.InnerCornerRadius == g_MaskingInfo.CornerRadius) ?
			dist : distanceFromRoundedRect(g_MaskingInfo.EdgeOffset, g_MaskingInfo.InnerCornerRadius);

		// v_BlendRange is set from outside in a hacky way to tell us the g_MaskingInfo.MaskingBlendRange used for the rounded
		// corners of the edge effect container itself. We can then derive the alpha factor for smooth inner edge
		// effect from that.
		highp float innerBlendFactor = (g_MaskingInfo.InnerCornerRadius - g_MaskingInfo.MaskingBlendRange - innerDist) / v_BlendRange.x;
		if (innerBlendFactor > 1.0)
		{
			return vec4(0.0);
		}

		// We exponentiate our factor to exactly counteract the later exponentiation by g_MaskingInfo.AlphaExponent for a smoother inner border.
		alphaFactor = pow(min(1.0 - innerBlendFactor, 1.0), 1.0 / g_MaskingInfo.AlphaExponent);
	}

	dist /= g_MaskingInfo.MaskingBlendRange;

	// This correction is needed to avoid fading of the alpha value for radii below 1px.
	highp float radiusCorrection = g_MaskingInfo.CornerRadius <= 0.0 ? g_MaskingInfo.MaskingBlendRange : max(0.0, g_MaskingInfo.MaskingBlendRange - g_MaskingInfo.CornerRadius);
	highp float fadeStart = (g_MaskingInfo.CornerRadius + radiusCorrection) / g_MaskingInfo.MaskingBlendRange;
	alphaFactor *= min(fadeStart - dist, 1.0);

	if (v_BlendRange.x > 0.0 || v_BlendRange.y > 0.0)
	{
		alphaFactor *= clamp(1.0 - distanceFromDrawingRect(texCoord), 0.0, 1.0);
	}

	if (alphaFactor <= 0.0)
	{
		return vec4(0.0);
	}

	// This ends up softening glow without negatively affecting edge smoothness much.
	alphaFactor = pow(alphaFactor, g_MaskingInfo.AlphaExponent);

	highp float borderStart = 1.0 + fadeStart - g_MaskingInfo.BorderThickness;
	lowp float colourWeight = min(borderStart - dist, 1.0);

	lowp vec4 contentColour = v_Colour * texel;

	if (colourWeight == 1.0)
		return vec4(contentColour.rgb, contentColour.a * alphaFactor);

	lowp vec4 borderColour = getBorderColour();

	if (colourWeight <= 0.0)
		return vec4(borderColour.rgb, borderColour.a * alphaFactor);

	contentColour.a *= alphaFactor;
	borderColour.a *= 1.0 - colourWeight;
	return blend(borderColour, contentColour);
}

#endif