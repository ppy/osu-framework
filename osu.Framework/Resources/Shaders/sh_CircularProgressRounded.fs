#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

#define PI 3.1415926538

varying mediump vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;
uniform mediump float progress;
uniform mediump float innerRadius;

bool insideProgressSector(vec2 pixelPos)
{
    if (progress >= 1.0)
        return true;

    float angle = atan(0.5 - pixelPos.y, 0.5 - pixelPos.x) - PI / 2.0;

    if (angle < 0.0)
        angle += 2.0 * PI;

    return angle < 2.0 * PI * progress;
}

bool insideProgress(vec2 pixelPos)
{
    float innerBorder = 0.5 - (0.5 * innerRadius);
    float outerBorder = 0.5;

    float dstFromCentre = distance(pixelPos, vec2(0.5));
    bool insideRing = dstFromCentre > innerBorder && dstFromCentre < outerBorder;

    return insideRing && insideProgressSector(pixelPos);
}

void main(void)
{
    if (progress == 0.0 || innerRadius == 0.0)
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    vec2 resolution = vec2(v_TexRect[2] - v_TexRect[0], v_TexRect[3] - v_TexRect[1]);
    vec2 pixelPos = v_TexCoord / resolution;
    
    if (!insideProgress(pixelPos))
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    gl_FragColor = getRoundedColor(toSRGB(wrappedSampler(wrappedCoord, v_TexRect, m_Sampler, -0.9)), wrappedCoord);
}
