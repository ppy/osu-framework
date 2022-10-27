#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"
#include "sh_BlobUtils.h"

varying highp vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;
uniform mediump float innerRadius;
uniform mediump float frequency;
uniform mediump float amplitude;
uniform int seed;
uniform highp float texelSize;

void main(void)
{
    if (innerRadius == 0.0)
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = v_TexCoord / resolution;
    
    highp vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    lowp vec4 textureColour = getRoundedColor(toSRGB(wrappedSampler(wrappedCoord, v_TexRect, m_Sampler, -0.9)), wrappedCoord);

    gl_FragColor = vec4(textureColour.rgb, textureColour.a * blobAlphaAt(pixelPos, innerRadius, texelSize, frequency, amplitude, seed));
}
