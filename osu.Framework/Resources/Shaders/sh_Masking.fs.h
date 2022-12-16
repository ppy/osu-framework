#version 130

varying lowp vec4 v_Colour;

#ifdef HIGH_PRECISION_VERTEX
	varying highp vec4 v_TexRect;
#else
	varying mediump vec4 v_TexRect;
#endif

varying mediump vec2 v_BlendRange;

in highp vec2 v_MaskingPosition;
in lowp vec4 v_BorderColour;

flat in highp float g_CornerRadius;
flat in highp float g_CornerExponent;
flat in highp vec4 g_MaskingRect;
flat in highp float g_BorderThickness;
flat in lowp mat4 g_BorderColour;
flat in mediump float g_MaskingBlendRange;
flat in lowp float g_AlphaExponent;
flat in highp vec2 g_EdgeOffset;
flat in lowp float g_DiscardInner;
flat in highp float g_InnerCornerRadius;
flat in highp mat3 g_ToMaskingSpace;
flat in lowp float g_IsMasking;

highp float distanceFromRoundedRect(highp vec2 offset, highp float radius)
{
    highp vec2 maskingPosition = v_MaskingPosition + offset;

    // Compute offset distance from masking rect in masking space.
    highp vec2 topLeftOffset = g_MaskingRect.xy - maskingPosition;
    highp vec2 bottomRightOffset = maskingPosition - g_MaskingRect.zw;

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
        return pow(pow(distanceFromShrunkRect.x, g_CornerExponent) + pow(distanceFromShrunkRect.y, g_CornerExponent), 1.0 / g_CornerExponent);
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

lowp vec4 getRoundedColor(lowp vec4 texel, mediump vec2 texCoord)
{
    if (g_IsMasking == 0.0 && v_BlendRange == vec2(0.0))
    {
        return toSRGB(v_Colour * texel);
    }

    highp float dist = distanceFromRoundedRect(vec2(0.0), g_CornerRadius);
    lowp float alphaFactor = 1.0;

    // Discard inner pixels
    if (g_DiscardInner != 0.0)
    {
        highp
        float innerDist = (g_EdgeOffset == vec2(0.0) && g_InnerCornerRadius == g_CornerRadius) ? dist : distanceFromRoundedRect(g_EdgeOffset, g_InnerCornerRadius);

        // v_BlendRange is set from outside in a hacky way to tell us the g_MaskingBlendRange used for the rounded
        // corners of the edge effect container itself. We can then derive the alpha factor for smooth inner edge
        // effect from that.
        highp
        float innerBlendFactor = (g_InnerCornerRadius - g_MaskingBlendRange - innerDist) / v_BlendRange.x;
        if (innerBlendFactor > 1.0)
        {
            return vec4(0.0);
        }

        // We exponentiate our factor to exactly counteract the later exponentiation by g_AlphaExponent for a smoother inner border.
        alphaFactor = pow(min(1.0 - innerBlendFactor, 1.0), 1.0 / g_AlphaExponent);
    }

    dist /= g_MaskingBlendRange;

    // This correction is needed to avoid fading of the alpha value for radii below 1px.
    highp float radiusCorrection = g_CornerRadius <= 0.0 ? g_MaskingBlendRange : max(0.0, g_MaskingBlendRange - g_CornerRadius);
    highp float fadeStart = (g_CornerRadius + radiusCorrection) / g_MaskingBlendRange;
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
    alphaFactor = pow(alphaFactor, g_AlphaExponent);

    highp float borderStart = 1.0 + fadeStart - g_BorderThickness;
    lowp float colourWeight = min(borderStart - dist, 1.0);

    if (colourWeight <= 0.0)
    {
        return toSRGB(vec4(v_BorderColour.rgb, v_BorderColour.a * alphaFactor));
    }

    lowp vec4 dest = vec4(v_Colour.rgb, v_Colour.a * alphaFactor) * texel;
    lowp vec4 src = vec4(v_BorderColour.rgb, v_BorderColour.a * (1.0 - colourWeight));

    return blend(toSRGB(src), toSRGB(dest));
}
