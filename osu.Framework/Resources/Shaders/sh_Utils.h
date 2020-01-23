#define GAMMA 2.4

uniform bool g_GammaCorrection;

lowp float toLinear(lowp float color)
{
	return color <= 0.04045 ? (color / 12.92) : pow((color + 0.055) / 1.055, GAMMA);
}

lowp vec4 toLinear(lowp vec4 colour)
{
	return vec4(toLinear(colour.r), toLinear(colour.g), toLinear(colour.b), colour.a);
}

lowp float toSRGB(lowp float color)
{
	return color < 0.0031308 ? (12.92 * color) : (1.055 * pow(color, 1.0 / GAMMA) - 0.055);
}

lowp vec4 toSRGB(lowp vec4 colour)
{
#ifdef GL_ES
 	return g_GammaCorrection ? vec4(toSRGB(colour.r), toSRGB(colour.g), toSRGB(colour.b), colour.a) : colour;
#else
	return vec4(toSRGB(colour.r), toSRGB(colour.g), toSRGB(colour.b), colour.a);
#endif
	// The following implementation using mix and step may be faster, but stackoverflow indicates it is in fact a lot slower on some GPUs.
	//return vec4(mix(colour.rgb * 12.92, 1.055 * pow(colour.rgb, vec3(1.0 / GAMMA)) - vec3(0.055), step(0.0031308, colour.rgb)), colour.a);
}

// perform alpha compositing of two colour components.
// see http://apoorvaj.io/alpha-compositing-opengl-blending-and-premultiplied-alpha.html
lowp vec4 blend(lowp vec4 src, lowp vec4 dst)
{
    lowp float finalAlpha = src.a + dst.a * (1.0 - src.a);

    if (finalAlpha == 0.0)
        return vec4(0);

    return vec4(
        (src.rgb * src.a + dst.rgb * dst.a * (1.0 - src.a)) / finalAlpha,
        finalAlpha
    );
}