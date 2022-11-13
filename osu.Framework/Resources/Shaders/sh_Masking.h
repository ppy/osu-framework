varying highp vec2 v_Position;
varying lowp vec4 v_Colour;

#ifdef HIGH_PRECISION_VERTEX
	varying highp vec4 v_TexRect;
#else
	varying mediump vec4 v_TexRect;
#endif

varying mediump vec2 v_BlendRange;
varying highp vec2 v_MaskingTexCoord;

highp vec2 v_MaskingPosition;
highp float g_CornerRadius;
highp float g_CornerExponent;
highp vec4 g_MaskingRect;
highp float g_BorderThickness;
lowp mat4 g_BorderColour;
mediump float g_MaskingBlendRange;
lowp float g_AlphaExponent;
highp vec2 g_EdgeOffset;
lowp float g_DiscardInner;
highp float g_InnerCornerRadius;
highp mat3 g_ToMaskingSpace;
bool g_IsMasking;

uniform highp vec2 g_MaskingTexSize;
uniform highp sampler2D g_MaskingBlockSampler;

vec4 maskingTex(int texIndex)
{
    return texture2D(g_MaskingBlockSampler, (v_MaskingTexCoord + vec2(float(texIndex), 0.0) + vec2(0.5)) / g_MaskingTexSize);
}

void initMasking()
{
    g_MaskingRect = maskingTex(0);

    g_ToMaskingSpace[0][0] = maskingTex(1).r;
    g_ToMaskingSpace[0][1] = maskingTex(1).g;
    g_ToMaskingSpace[0][2] = maskingTex(1).b;
    g_ToMaskingSpace[1][0] = maskingTex(1).a;

    g_ToMaskingSpace[1][1] = maskingTex(2).r;
    g_ToMaskingSpace[1][2] = maskingTex(2).g;
    g_ToMaskingSpace[2][0] = maskingTex(2).b;
    g_ToMaskingSpace[2][1] = maskingTex(2).a;

    g_ToMaskingSpace[2][2] = maskingTex(3).r;
    g_CornerRadius = maskingTex(3).g;
    g_CornerExponent = maskingTex(3).b;
    g_BorderThickness = maskingTex(3).a;

    g_BorderColour[0] = maskingTex(4);
    g_BorderColour[1] = maskingTex(5);
    g_BorderColour[2] = maskingTex(6);
    g_BorderColour[3] = maskingTex(7);

    g_MaskingBlendRange = maskingTex(8).r;
    g_AlphaExponent = maskingTex(8).g;
    g_EdgeOffset = maskingTex(8).ba;

    g_DiscardInner = maskingTex(9).r;
    g_InnerCornerRadius = maskingTex(9).g;
    g_IsMasking = maskingTex(9).b == 1.0;

    // Transform from screen space to masking space.
    highp vec3 maskingPos = g_ToMaskingSpace * vec3(v_Position, 1.0);
    v_MaskingPosition = maskingPos.xy / maskingPos.z;
}

highp float distanceFromRoundedRect(highp vec2 offset, highp float radius)
{
    highp vec2 maskingPosition = v_MaskingPosition + offset;

    // Compute offset distance from masking rect in masking space.
    highp vec2 topLeftOffset = g_MaskingRect.xy - maskingPosition;
    highp vec2 bottomRightOffset = maskingPosition - g_MaskingRect.zw;

    highp vec2 distanceFromShrunkRect = max(
        bottomRightOffset + vec2(radius),
        topLeftOffset + vec2(radius));

    highp
    float maxDist = max(distanceFromShrunkRect.x, distanceFromShrunkRect.y);

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

lowp vec4 getBorderColour()
{
    highp vec2 relativeTexCoord = v_MaskingPosition / (g_MaskingRect.zw - g_MaskingRect.xy);
    lowp vec4 top = mix(g_BorderColour[0], g_BorderColour[2], relativeTexCoord.x);
    lowp vec4 bottom = mix(g_BorderColour[1], g_BorderColour[3], relativeTexCoord.x);
    return mix(top, bottom, relativeTexCoord.y);
}

lowp vec4 getRoundedColor(lowp vec4 texel, mediump vec2 texCoord)
{
    if (!g_IsMasking && v_BlendRange == vec2(0.0))
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

    highp
    float borderStart = 1.0 + fadeStart - g_BorderThickness;
    lowp
    float colourWeight = min(borderStart - dist, 1.0);

    lowp vec4 borderColour = getBorderColour();

    if (colourWeight <= 0.0)
    {
        return toSRGB(vec4(borderColour.rgb, borderColour.a * alphaFactor));
    }

    lowp vec4 dest = vec4(v_Colour.rgb, v_Colour.a * alphaFactor) * texel;
    lowp vec4 src = vec4(borderColour.rgb, borderColour.a * (1.0 - colourWeight));

    return blend(toSRGB(src), toSRGB(dest));
}
