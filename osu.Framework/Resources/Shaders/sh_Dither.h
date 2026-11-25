#ifndef DITHER_H
#define DITHER_H

// Impelements Bayer matrix (optimal matrix for ordered dithering) per the bitwise algorithm from here:
// https://en.wikipedia.org/wiki/Ordered_dithering . Bitwise operations are expanded and simplified as
// far as possible for shader code. The floating point offsets and scales are chosen such that the dither
// values are in the exclusive range (-0.5 / 255.0, 0.5 / 255.0), meaning that already-quantized values,
// such as ones coming from RGBA8 textures, will not be affected at all.

lowp float dither2() {
	ivec2 p = ivec2(gl_FragCoord.xy) & 3;
	int z = (p.x ^ p.y) & 1;
	int v = (z << 1) | (p.y & 1);
	return (float(v) - 1.5) * (1.0 / 1020.0);
}

lowp float dither4() {
	ivec2 p = ivec2(gl_FragCoord.xy) & 3;
	int z = p.x ^ p.y;
	int v = ((z & 1) << 3) | ((p.y & 1) << 2) | (z & 2) | ((p.y & 2) >> 1);
	return (float(v) - 7.5) * (1.0 / 4080.0);
}

lowp float dither8() {
	ivec2 p = ivec2(gl_FragCoord.xy) & 7;
	int z = p.x ^ p.y;
	int v = ((z & 1) << 5) | ((p.y & 1) << 4)
	      | ((z & 2) << 2) | ((p.y & 2) << 1)
	      | ((z & 4) >> 1) | ((p.y & 4) >> 2);
	return (float(v) - 31.5) * (1.0 / 16320.0); 
}

lowp vec4 dither(vec4 colour) {
	float ditherAmount = dither4(); // 4x4 seems to be a good quality/performance balance

	// NOTE: premultiplied alpha would simplify this to colour-only dithering with one less divide:
	// return vec4(colour.rgb + ditherAmount, colour.a);
	// However, with non-premultiplied alpha, alpha gets quantized prior to being multiplied with the colour in the
	// blending stage, hence we additionally dither alpha.
	return colour.a >= 1.0 / 255.0 ? vec4(colour.rgb + ditherAmount / colour.a, colour.a + ditherAmount) : colour;
}

#endif
