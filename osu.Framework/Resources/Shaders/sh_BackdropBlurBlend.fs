#ifndef BLUR_FS
#define BLUR_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

#undef INV_SQRT_2PI
#define INV_SQRT_2PI 0.39894

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_BlendParameters
{
	lowp float g_MaskCutoff;
	lowp float g_BackdropOpacity;
	lowp float g_BackdropTintStrength;
};

layout(set = 2, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 2, binding = 1) uniform lowp sampler m_Sampler;

layout(set = 3, binding = 0) uniform lowp texture2D m_Mask;
layout(set = 3, binding = 1) uniform lowp sampler m_MaskSampler;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
	vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);

	vec4 foreground = wrappedSampler(wrappedCoord, v_TexRect, m_Mask, m_MaskSampler, -0.9);

	if (foreground.a > g_MaskCutoff) {
		foreground *= v_Colour;

		vec4 background = wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9) * g_BackdropOpacity;

		if (background.a > 0) {

			background.rgb = mix(background.rgb / background.a, background.rgb / background.a * foreground.rgb / foreground.a, g_BackdropTintStrength * foreground.a) * background.a;

			float alpha = background.a + (1.0 - background.a) * foreground.a;

			o_Colour = vec4(mix(background.rgb, foreground.rgb, foreground.a) / alpha, alpha);
		} else {
			o_Colour = foreground * v_Colour;
		}

	} else {
		o_Colour = foreground * v_Colour;
	}
}

#endif