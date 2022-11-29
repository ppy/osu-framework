#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"
#include "sh_CircularProgressUtils.h"

varying highp vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;
uniform mediump float progress;
uniform mediump float innerRadius;
uniform highp float texelSize;
uniform bool roundedCaps;

void main(void)
{
    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = v_TexCoord / resolution;
    
    highp vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    lowp vec4 textureColour = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Sampler, -0.9), wrappedCoord);

    gl_FragColor = vec4(textureColour.rgb, textureColour.a * progressAlphaAt(pixelPos, progress, innerRadius, roundedCaps, texelSize));
}
