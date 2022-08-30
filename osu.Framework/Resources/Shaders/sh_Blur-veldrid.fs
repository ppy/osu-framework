#include "sh_Utils.h"

#define INV_SQRT_2PI 0.39894

layout(location = 0) in highp vec2 v_MaskingPosition;
layout(location = 1) in lowp vec4 v_Colour;
layout(location = 2) in mediump vec2 v_TexCoord;
layout(location = 3) in mediump vec4 v_TexRect;
layout(location = 4) in mediump vec2 v_BlendRange;

layout(set = 1, binding = 0) uniform texture2D m_Texture;
layout(set = 1, binding = 1) uniform sampler m_Sampler;

uniform mediump vec2 g_TexSize;
uniform int g_Radius;
uniform mediump float g_Sigma;
uniform highp vec2 g_BlurDirection;

layout(location = 0) out vec4 o_Colour;

mediump float computeGauss(in mediump float x, in mediump float sigma)
{
	return INV_SQRT_2PI * exp(-0.5*x*x / (sigma*sigma)) / sigma;
}

lowp vec4 blur(texture2D tex, sampler samp, int radius, highp vec2 direction, mediump vec2 texCoord, mediump vec2 texSize, mediump float sigma)
{
	mediump float factor = computeGauss(0.0, sigma);
	mediump vec4 sum = texture(sampler2D(tex, samp), texCoord) * factor;

	mediump float totalFactor = factor;

	for (int i = 2; i <= 200; i += 2)
	{
		mediump float x = float(i) - 0.5;
		factor = computeGauss(x, sigma) * 2.0;
		totalFactor += 2.0 * factor;
		sum += texture(sampler2D(tex, samp), texCoord + direction * x / texSize) * factor;
		sum += texture(sampler2D(tex, samp), texCoord - direction * x / texSize) * factor;
		if (i >= radius)
			break;
	}

    return toSRGB(sum / totalFactor);
}

void main(void)
{
	o_Colour = blur(m_Texture, m_Sampler, g_Radius, g_BlurDirection, v_TexCoord, g_TexSize, g_Sigma);
}