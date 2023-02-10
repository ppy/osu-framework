// This file is automatically included in every shader.

/**
* \brief Retrieves the set number for a uniform layout, offset by any internal framework layouts.
 * \param a The desired set number.
 */
#define BASE_SET_OFFSET(a) (a + 1)

/**
 * \brief Creates a uniform layout definition bound to binding 0 in the given set.
 *  This calls BASE_SET_OFFSET(set_num) internally.
 * \param set_num The desired set number.
 */
#define UNIFORM_BLOCK(set_num, uniform_name) layout(std140, set = BASE_SET_OFFSET(set_num), binding = 0) uniform uniform_name

#define UNIFORM_TEXTURE(set_num, texture_name, sampler_name) \
 layout(set = BASE_SET_OFFSET(set_num), binding = 0) uniform lowp texture2D texture_name; \
 layout(set = BASE_SET_OFFSET(set_num), binding = 1) uniform lowp sampler sampler_name

UNIFORM_BLOCK(-1, g_GlobalUniforms)
{
    bool g_GammaCorrection;

    // Whether the backbuffer is currently being drawn to.
    bool g_BackbufferDraw;

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