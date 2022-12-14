#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"
#include "sh_CircularBlobUtils.h"

varying highp vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;
uniform mediump float innerRadius;
uniform int pointCount;
uniform mediump float amplitude;
uniform highp vec2 noisePosition;
uniform highp float texelSize;

void main(void)
{
    highp vec2 pixelPos = v_TexCoord / (v_TexRect.zw - v_TexRect.xy);

    if (distance(pixelPos, vec2(0.5)) > 0.5)
    {
        gl_FragColor = vec4(0.0);
        return;
    }
    
    highp vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    lowp vec4 textureColour = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Sampler, -0.9), wrappedCoord);

    gl_FragColor = vec4(textureColour.rgb, textureColour.a * blobAlphaAt(pixelPos, innerRadius, texelSize, pointCount, amplitude, noisePosition));
}
