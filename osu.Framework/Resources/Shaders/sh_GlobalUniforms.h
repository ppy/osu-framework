// This file is automatically included in every shader.

uniform bool g_GammaCorrection;

// Whether the backbuffer is currently being drawn to.
uniform bool g_BackbufferDraw;

uniform mat4 g_ProjMatrix;
uniform mat3 g_ToMaskingSpace;

uniform bool g_IsMasking;
uniform highp float g_CornerRadius;
uniform highp float g_CornerExponent;
uniform highp vec4 g_MaskingRect;
uniform highp float g_BorderThickness;
uniform lowp mat4 g_BorderColour;
uniform mediump float g_MaskingBlendRange;
uniform lowp float g_AlphaExponent;
uniform highp vec2 g_EdgeOffset;
uniform bool g_DiscardInner;
uniform highp float g_InnerCornerRadius;

// 0 -> None
// 1 -> ClampToEdge
// 2 -> ClampToBorder
// 3 -> Repeat
uniform int g_WrapModeS;
uniform int g_WrapModeT;