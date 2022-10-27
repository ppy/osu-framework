#include "sh_Utils.h"
#include "sh_TextureWrapping.h"
#include "sh_BlobUtils.h"

varying lowp vec4 v_Colour;
varying highp vec2 v_TexCoord;
varying highp vec4 v_TexRect;

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
    
    lowp vec4 textureColour = toSRGB(v_Colour * wrappedSampler(wrap(v_TexCoord, v_TexRect), v_TexRect, m_Sampler, -0.9));
    gl_FragColor = vec4(textureColour.rgb, textureColour.a * blobAlphaAt(pixelPos, innerRadius, texelSize, frequency, amplitude, seed));
}
