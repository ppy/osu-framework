#ifndef CIRCULAR_PROGRESS_EDGE_EFFECT_FS
#define CIRCULAR_PROGRESS_EDGE_EFFECT_FS

#undef HIGH_PRECISION_VERTEX
#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_CircularProgressUtils.h"

layout(location = 2) in highp vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_CircularProgressEdgeEffectParameters
{
    highp vec2 glowSize;
    mediump float innerRadius;
    mediump float progress;
    highp float texelSize;
    bool roundedCaps;
    bool hollow;
};

layout(location = 0) out vec4 o_Colour;

lowp float getGlow(highp float distance, highp float size)
{
    // Many sources suggest that y = 1/x function looks the best for glow and imo it really does.
    // We will pick a part of it with x ranging from 0.4 to 5.0, but these values can be adjusted.

    const float lowerX = 0.4;
    const float higherX = 5.0;

    highp float ratio = clamp(distance, 0.0, size) / size; // how far away we are from the object within glow (from 0 to 1)
    highp float x = lowerX + ratio * (higherX - lowerX);
    highp float glow = 1.0 / x * lowerX; // adjust to be within 0..1 range
    return glow * (1.0 - ratio); // function won't reach 0, add linear fade on top
}

lowp float getBlur(highp float distance, highp float size)
{
    return smoothstep(size, -size, distance);
}

lowp float getGlowDebug(highp float distance, highp float size)
{
    if (distance < 0.0)
        return 1.0;

    if (distance < size)
        return 0.5;

    return 0.0;
}

void main(void)
{
    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;

    // Inflate coordinate space, so it would be (-glowSize -> 0 -> 1 -> glowSize) to preserve everything in place while inflating the draw quad
    highp vec2 pixelPos = (v_TexCoord / resolution) * (vec2(1.0) + glowSize * 2.0) - glowSize;

    highp float dst = distanceToProgress(pixelPos, progress, innerRadius, roundedCaps, texelSize);
    lowp float glowA = hollow && dst < 0.0 ? smoothstep(texelSize, 0.0, -dst) : getGlow(dst, min(glowSize.x, glowSize.y));
    o_Colour = getRoundedColor(vec4(vec3(1.0), glowA), v_TexCoord);
}

#endif