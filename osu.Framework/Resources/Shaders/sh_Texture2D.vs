#include "sh_Utils.h"

// Vertex inputs.
attribute highp vec2 m_Position;
attribute lowp vec4 m_Colour;
attribute mediump vec2 m_TexCoord;
attribute mediump vec4 m_TexRect;
attribute mediump vec2 m_BlendRange;
attribute highp vec2 m_MaskingTexCoord;

// Vertex outputs.
varying highp vec2 v_Position;
varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;
varying mediump vec2 v_BlendRange;

// Vertex outputs (from the masking texture).
varying highp vec2 v_MaskingPosition;
varying highp float g_CornerRadius;
varying highp float g_CornerExponent;
varying highp vec4 g_MaskingRect;
varying highp float g_BorderThickness;
varying lowp mat4 g_BorderColour;
varying lowp vec4 v_BorderColour;
varying mediump float g_MaskingBlendRange;
varying lowp float g_AlphaExponent;
varying highp vec2 g_EdgeOffset;
varying lowp float g_DiscardInner;
varying highp float g_InnerCornerRadius;
varying highp mat3 g_ToMaskingSpace;
varying lowp float g_IsMasking;

uniform highp mat4 g_ProjMatrix;
uniform highp vec2 g_MaskingTexSize;
uniform highp sampler2D g_MaskingBlockSampler;

vec4 maskingTex(int texIndex)
{
    return texture2D(g_MaskingBlockSampler, (m_MaskingTexCoord + vec2(float(texIndex), 0.0) + vec2(0.5)) / g_MaskingTexSize);
}

lowp vec4 getBorderColour()
{
    highp vec2 relativeTexCoord = v_MaskingPosition / (g_MaskingRect.zw - g_MaskingRect.xy);
    lowp vec4 top = mix(g_BorderColour[0], g_BorderColour[2], relativeTexCoord.x);
    lowp vec4 bottom = mix(g_BorderColour[1], g_BorderColour[3], relativeTexCoord.x);
    return mix(top, bottom, relativeTexCoord.y);
}

void initMasking()
{
    vec4 t0 = maskingTex(0);

    g_IsMasking = t0.r;
    if (g_IsMasking == 0.0 && m_BlendRange == vec2(0.0)) {
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
    highp vec3 maskingPos = g_ToMaskingSpace * vec3(m_Position, 1.0);
    v_MaskingPosition = maskingPos.xy / maskingPos.z;

    v_BorderColour = getBorderColour();
}

void main(void)
{
    v_Position = m_Position;
	v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	v_TexRect = m_TexRect;
	v_BlendRange = m_BlendRange;

	gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
	
	initMasking();
}
