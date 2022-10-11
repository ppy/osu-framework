#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"
#include "sh_CircularProgressUtils.h"

varying highp vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;
uniform mediump float progress;
uniform mediump float innerRadius;
uniform bool roundedCaps;

void main(void)
{
    if (progress == 0.0 || innerRadius == 0.0)
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = v_TexCoord / resolution;
    
    if (!insideProgress(pixelPos, progress, innerRadius, roundedCaps))
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    highp vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    gl_FragColor = getRoundedColor(toSRGB(wrappedSampler(wrappedCoord, v_TexRect, m_Sampler, -0.9)), wrappedCoord);
}
