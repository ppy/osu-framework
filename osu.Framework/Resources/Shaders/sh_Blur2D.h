#ifdef GL_ES
precision mediump float;
#endif

#include "sh_Utils.h"

#define INV_SQRT_2PI 0.39894

float computeGauss(in float x, in float sigma)
{
	return INV_SQRT_2PI * exp(-0.5*x*x / (sigma*sigma)) / sigma;
}

vec4 blur(sampler2D tex, int radius, vec2 direction, vec2 texCoord, vec2 texSize, float sigma)
{
	float factor = computeGauss(0.0, sigma);
	vec4 sum = texture2D(tex, texCoord) * factor;

	float totalFactor = factor;

	for (int i = 2; i <= 200; i += 2)
	{
		float x = float(i) - 0.5;
		factor = computeGauss(x, sigma) * 2.0;
		totalFactor += 2.0 * factor;
		sum += texture2D(tex, texCoord + direction * x / texSize) * factor;
		sum += texture2D(tex, texCoord - direction * x / texSize) * factor;
		if (i >= radius)
			break;
	}

    return toSRGB(sum / totalFactor);
}