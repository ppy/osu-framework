#define GAMMA 2.4

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