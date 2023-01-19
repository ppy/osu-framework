#version 130

attribute highp int m_MaskingId;

out highp vec2 v_MaskingPosition;
out lowp vec4 v_BorderColour;

flat out highp float g_CornerRadius;
flat out highp float g_CornerExponent;
flat out highp vec4 g_MaskingRect;
flat out highp float g_BorderThickness;
flat out lowp mat4 g_BorderColour;
flat out mediump float g_MaskingBlendRange;
flat out lowp float g_AlphaExponent;
flat out highp vec2 g_EdgeOffset;
flat out lowp float g_DiscardInner;
flat out highp float g_InnerCornerRadius;
flat out highp mat3 g_ToMaskingSpace;
flat out lowp float g_IsMasking;

uniform highp sampler2D g_MaskingBlockSampler;

vec4 maskingTex(int texIndex)
{
    return texelFetch(g_MaskingBlockSampler, ivec2(texIndex, m_MaskingId), 0);
}

lowp vec4 getBorderColour()
{
    highp vec2 relativeTexCoord = v_MaskingPosition / (g_MaskingRect.zw - g_MaskingRect.xy);
    lowp vec4 top = mix(g_BorderColour[0], g_BorderColour[2], relativeTexCoord.x);
    lowp vec4 bottom = mix(g_BorderColour[1], g_BorderColour[3], relativeTexCoord.x);
    return mix(top, bottom, relativeTexCoord.y);
}

void initMasking(vec3 position)
{
    vec4 t0 = maskingTex(0);

    g_IsMasking = t0.r;
    if (g_IsMasking == 0.0 && v_BlendRange == vec2(0.0)) {
        return;
    }
    
    vec4 t1 = maskingTex(1);
    vec4 t2 = maskingTex(2);
    vec4 t3 = maskingTex(3);
    vec4 t4 = maskingTex(4);
    vec4 t5 = maskingTex(5);
    vec4 t6 = maskingTex(6);
    vec4 t7 = maskingTex(7);
    vec4 t8 = maskingTex(8);
    vec4 t9 = maskingTex(9);

    g_ToMaskingSpace[0][0] = t0.g;
    g_ToMaskingSpace[0][1] = t0.b;
    g_ToMaskingSpace[0][2] = t0.a;

    g_ToMaskingSpace[1][0] = t1.r;
    g_ToMaskingSpace[1][1] = t1.g;
    g_ToMaskingSpace[1][2] = t1.b;
    g_ToMaskingSpace[2][0] = t1.a;

    g_ToMaskingSpace[2][1] = t2.r;
    g_ToMaskingSpace[2][2] = t2.g;
    g_CornerRadius = t2.b;
    g_CornerExponent = t2.a;

    g_MaskingRect = t3;

    g_BorderColour[0] = t4;
    g_BorderColour[1] = t5;
    g_BorderColour[2] = t6;
    g_BorderColour[3] = t7;

    g_BorderThickness = t8.r;
    g_MaskingBlendRange = t8.g;
    g_AlphaExponent = t8.b;
    g_DiscardInner = t8.a;

    g_EdgeOffset = t9.rg;
    g_InnerCornerRadius = t9.b;

    // Transform from screen space to masking space.
    highp vec3 maskingPos = g_ToMaskingSpace * position;
    v_MaskingPosition = maskingPos.xy / maskingPos.z;

    v_BorderColour = getBorderColour();
}