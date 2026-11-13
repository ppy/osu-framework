#ifndef UTILS_H
#define UTILS_H

#undef GAMMA
#define GAMMA 2.4

// Perform alpha compositing of two colour components. Assumed both are linear with premultiplied alpha.
// The linearity assumption is sometimes broken in practice (IIRC because it produces nicer looking blends
// than the physically correct linear blend), but alpha premultiplication must always be satisfied.
lowp vec4 blend(lowp vec4 src, lowp vec4 dst)
{
    return src + dst * (1.0 - src.a);
}

lowp vec4 premul(lowp vec4 colour)
{
    return vec4(colour.rgb * colour.a, colour.a);
}

lowp vec4 toEmissive(bool isEmissive, lowp vec4 colour)
{
    return vec4(colour.rgb, isEmissive ? 0.0 : colour.a);
}

// http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
// slightly amended to also handle alpha
vec4 hsv2rgb(vec4 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return vec4(c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y), c.w);
}

// decodes 24-bit float from 4 8-bit floats (https://aras-p.info/blog/2009/07/30/encoding-floats-to-rgba-the-final/)
highp float decodeFloat(highp vec4 frameBuffer)
{
    return dot(frameBuffer, vec4(1.0, 1/255.0, 1/65025.0, 1/16581375.0));
}

// encodes 24-bit [0, 1) float to 4 8-bit floats (https://aras-p.info/blog/2009/07/30/encoding-floats-to-rgba-the-final/)
lowp vec4 encodeFloat(highp float value)
{
    highp vec4 enc = value * vec4(1.0, 255.0, 65025.0, 16581375.0);
    enc = fract(enc);
    enc -= enc.yzww * vec4(1.0/255.0, 1.0/255.0, 1.0/255.0, 0.0);
    return enc;
}

#endif
