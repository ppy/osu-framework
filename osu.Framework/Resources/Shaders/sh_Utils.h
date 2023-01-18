#define GAMMA 2.4

lowp float toLinear(lowp float color)
{
    return color <= 0.04045 ? (color / 12.92) : pow((color + 0.055) / 1.055, GAMMA);
}

lowp vec4 toLinear(lowp vec4 colour)
{
#ifdef GL_ES
    return g_GammaCorrection ? vec4(toLinear(colour.r), toLinear(colour.g), toLinear(colour.b), colour.a) : colour;
#else
    return vec4(toLinear(colour.r), toLinear(colour.g), toLinear(colour.b), colour.a);
#endif
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

// http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
// slightly amended to also handle alpha
vec4 hsv2rgb(vec4 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return vec4(c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y), c.w);
}
