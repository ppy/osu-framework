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
    g_IsMasking = maskingTex(9).b;

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
