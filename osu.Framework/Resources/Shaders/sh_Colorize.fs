#ifndef COLORIZE_FS
#define COLORIZE_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

const mediump float luma_r = 0.299;
const mediump float luma_g = 0.587;
const mediump float luma_b = 0.114;

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_ColorizeParameters
{
    mediump vec4 g_Tint;
    int g_Algorithm;
    int g_Range;
    mediump float g_ChromaEpsilon;
    mediump float g_WhiteLuma;
    mediump float g_WhiteChroma;
    mediump float g_WhiteTolerance;
};

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

mediump float luma(mediump vec3 c)
{
    return dot(c, vec3(luma_r, luma_g, luma_b));
}

mediump float chroma(mediump vec3 c)
{
    return max(max(abs(c.r - c.g), abs(c.g - c.b)), abs(c.r - c.b));
}

mediump vec3 clipColor(mediump vec3 c)
{
    mediump float l = luma(c);
    mediump float n = min(min(c.r, c.g), c.b);
    mediump float x = max(max(c.r, c.g), c.b);
    const mediump float eps = 1.0e-6;

    if (n < 0.0)
    {
        mediump float t = l / max(l - n, eps);
        c = l + (c - l) * t;
        l = luma(c);
        x = max(max(c.r, c.g), c.b);
    }

    if (x > 1.0)
    {
        mediump float t = (1.0 - l) / max(x - l, eps);
        c = l + (c - l) * t;
    }

    return clamp(c, 0.0, 1.0);
}

mediump vec3 setLum(mediump vec3 tint, mediump float targetLuma)
{
    return clipColor(tint + (targetLuma - luma(tint)));
}

mediump float rangeMask(mediump vec3 straight)
{
    mediump float ch = chroma(straight);

    if (g_Range == 1)
        return ch <= g_ChromaEpsilon ? 1.0 : 0.0;

    if (g_Range == 2)
        return (luma(straight) < g_WhiteLuma || ch > g_WhiteChroma) ? 1.0 : 0.0;

    if (g_Range == 3)
    {
        mediump float deficit = 1.0 - min(min(straight.r, straight.g), straight.b);

        if (g_WhiteTolerance <= 0.0)
            return deficit <= 1.0e-6 ? 1.0 : 0.0;

        return clamp(1.0 - deficit / g_WhiteTolerance, 0.0, 1.0);
    }

    return 1.0;
}

void main(void)
{
    mediump vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    lowp vec4 texel = wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9);

    mediump float alpha = texel.a;
    mediump vec3 rgb = texel.rgb;

    if (alpha > 0.0 && alpha < 1.0)
        rgb /= alpha;

    mediump float mask = rangeMask(rgb);
    mediump vec3 recoloured = g_Algorithm == 1
        ? setLum(g_Tint.rgb, luma(rgb))
        : rgb * g_Tint.rgb;

    if (g_Range == 3 && g_Algorithm == 1)
        recoloured = mix(recoloured, g_Tint.rgb, mask);

    rgb = mix(rgb, recoloured, mask);

    if (alpha > 0.0 && alpha < 1.0)
        rgb *= alpha;

    o_Colour = getRoundedColor(vec4(rgb, alpha), wrappedCoord);
}

#endif
