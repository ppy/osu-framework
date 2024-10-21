#ifndef BLUR_FS
#define BLUR_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

#undef INV_SQRT_2PI
#define INV_SQRT_2PI 0.39894

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_BlurParameters
{
	mediump vec2 g_TexSize;
	int g_Radius;
	mediump float g_Sigma;
	highp vec2 g_BlurDirection;
	lowp float g_MaskCutoff;
	mediump float g_BlurResolution;
	lowp float g_BackdropOpacity;
	lowp float g_BackdropTintStrength;
};

layout(set = 2, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 2, binding = 1) uniform lowp sampler m_Sampler;

layout(set = 3, binding = 0) uniform lowp texture2D m_Mask;
layout(set = 3, binding = 1) uniform lowp sampler m_MaskSampler;


layout(location = 0) out vec4 o_Colour;

mediump float computeGauss(in mediump float x, in mediump float sigma)
{
	return INV_SQRT_2PI * exp(-0.5*x*x / (sigma*sigma)) / sigma;
}

lowp vec4 blur(int radius, highp vec2 direction, mediump vec2 texCoord, mediump vec2 texSize, mediump float sigma)
{
	mediump float factor = computeGauss(0.0, sigma);
	mediump vec4 sum = texture(sampler2D(m_Texture, m_Sampler), texCoord) * factor;

	mediump float totalFactor = factor;

	for (int i = 2; i <= 200; i += 2)
	{
		mediump float x = (float(i) - 0.5);
		factor = computeGauss(x, sigma) * 2.0;
		totalFactor += 2.0 * factor;
		x *= g_BlurResolution;
		sum += texture(sampler2D(m_Texture, m_Sampler), texCoord + direction * x / texSize) * factor;
		sum += texture(sampler2D(m_Texture, m_Sampler), texCoord - direction * x / texSize) * factor;
		if (i >= radius)
			break;
	}

	// this is supposed to be unnecessary with https://github.com/ppy/veldrid-spirv/pull/4,
	// but blurring is still broken on some Apple devices when removing it (at least on an M2 iPad Pro and an iPhone 12).
	// todo: investigate this.
	float one = g_BackbufferDraw ? 0 : 1;

	return sum / totalFactor * one;
}

void main(void)
{
	vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);

	vec4 foreground = wrappedSampler(wrappedCoord, v_TexRect, m_Mask, m_MaskSampler, -0.9);

	if (foreground.a > g_MaskCutoff) {
		foreground *= v_Colour;

		vec4 background = blur(g_Radius, g_BlurDirection, v_TexCoord, g_TexSize, g_Sigma) * g_BackdropOpacity;

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