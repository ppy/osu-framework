#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"
#include "sh_CircularBlobUtils.h"

varying highp vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;
uniform highp float pathRadius;
uniform int pointCount;
uniform mediump float amplitude;
uniform highp vec2 noisePosition;
uniform highp float texelSize;

void main(void)
{
    highp vec2 pixelPos = v_TexCoord / (v_TexRect.zw - v_TexRect.xy);

    highp float dstFromCentre = distance(pixelPos, vec2(0.5));

    highp float angle = HALF_PI * (1.0 - 2.0 / float(pointCount));
    highp float minHeight = 0.5 * sin(angle) * (1.0 - amplitude) - texelSize;
    highp float c = cos(angle);
    highp float maxHeight = 0.5 - 0.25 * c * c;

    // Hack to make the shape appear smooth if it's thickness < texelSize by making it more transparent while leaving thickness the same
    // Makes sense for thin path radius or small-sized drawable
    highp float subAAMultiplier = clamp(pathRadius / texelSize, 0.0, 1.0);
    pathRadius = max(pathRadius, texelSize);

    // Discard guaranteed empty pixels from inside and outside the shape.
    // Skips a lot of pixels in "low amplitude thin path radius" case.
    if (dstFromCentre > maxHeight || dstFromCentre < minHeight - pathRadius)
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    // Assume pixel is inside the path.
    lowp float alpha = 1.0;

    // Compute curve if it's outside guaranteed filled area.
    // Skips a lot of pixels in "low amplitude thick path radius" case.
    if (dstFromCentre > minHeight || dstFromCentre < maxHeight - pathRadius)
        alpha = blobAlphaAt(pixelPos, pathRadius, texelSize, pointCount, amplitude, noisePosition);

    highp vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    lowp vec4 texture = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Sampler, -0.9), wrappedCoord);

    gl_FragColor = vec4(texture.rgb, texture.a * alpha * subAAMultiplier);
}
