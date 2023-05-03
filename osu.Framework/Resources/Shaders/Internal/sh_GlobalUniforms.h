// This file is automatically included in every shader.

layout(std140, set = -1, binding = 0) uniform g_GlobalUniforms
{
    // Whether the backbuffer is currently being drawn to.
    bool g_BackbufferDraw;

    // Whether the depth values range from 0 to 1. If false, depth values range from -1 to 1.
    // OpenGL uses [-1, 1], Vulkan/D3D/MTL all use [0, 1].
    bool g_IsDepthRangeZeroToOne;

    // Whether the clip space ranges from -1 (top) to 1 (bottom). If false, the clip space ranges from -1 (bottom) to 1 (top).
    bool g_IsClipSpaceYInverted;

    // Whether the texture coordinates begin in the top-left of the texture. If false, (0, 0) is the bottom-left texel of the texture.
    bool g_IsUvOriginTopLeft;

    mat4 g_ProjMatrix;
    mat3 g_ToMaskingSpace;

    bool g_IsMasking;
    highp float g_CornerRadius;
    highp float g_CornerExponent;
    highp vec4 g_MaskingRect;
    highp float g_BorderThickness;
    lowp mat4 g_BorderColour;
    mediump float g_MaskingBlendRange;
    lowp float g_AlphaExponent;
    highp vec2 g_EdgeOffset;
    bool g_DiscardInner;
    highp float g_InnerCornerRadius;

    // 0 -> None
    // 1 -> ClampToEdge
    // 2 -> ClampToBorder
    // 3 -> Repeat
    int g_WrapModeS;
    int g_WrapModeT;
};