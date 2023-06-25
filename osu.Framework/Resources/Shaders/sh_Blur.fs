#ifndef BLUR_FS
#define BLUR_FS

#include "sh_Utils.h"

#undef INV_SQRT_2PI
#define INV_SQRT_2PI 0.39894

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_BlurParameters
{
	mediump vec2 g_TexSize;
	int g_Radius;
	mediump float g_Sigma;
	highp vec2 g_BlurDirection;
};

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;

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
		mediump float x = float(i) - 0.5;
		factor = computeGauss(x, sigma) * 2.0;
		totalFactor += 2.0 * factor;
		sum += texture(sampler2D(m_Texture, m_Sampler), texCoord + direction * x / texSize) * factor;
		sum += texture(sampler2D(m_Texture, m_Sampler), texCoord - direction * x / texSize) * factor;
		if (i >= radius)
			break;
	}

	// todo: workaround for a SPIR-V bug (https://github.com/ppy/osu-framework/issues/5719)
	float one = g_BackbufferDraw ? 0 : 1;

	return sum / totalFactor * one;
}

void main(void)
{
	o_Colour = blur(g_Radius, g_BlurDirection, v_TexCoord, g_TexSize, g_Sigma);
}

#endif