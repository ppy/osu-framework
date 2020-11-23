#include "sh_Utils.h"

#define SIN_PI_OVER_3 0.8660254037844386
#define TAN_PI_OVER_3 1.732050807568877

varying mediump vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;

// this is the "size" of the object in screen space--specifically max(width, height).
uniform highp float g_Resolution;

// a visual demonstration of this calculation can be found at https://www.desmos.com/calculator/vihhpowcrb.
highp float calculateHexagonDistance(mediump vec2 coord)
{
    mediump vec2 tmp = (coord - vec2(0.5));
    mediump vec2 norm = vec2(abs(tmp.x * 2.0), abs(tmp.y * 2.0));

    // d1 is the distance from coord to the right bound.
    highp float d1 = (-TAN_PI_OVER_3 * norm.x - norm.y + TAN_PI_OVER_3) / 2.0;
    // d1 is the distance from coord to the top bound.
    highp float d2 = SIN_PI_OVER_3 - norm.y;

    return min(d1, d2);
}

void main(void)
{
    gl_FragColor = toSRGB(texture2D(m_Sampler, v_TexCoord));

    // distance in pixels from the edge of the hexagon (assuming a square draw quad >_>).
    highp float distance = calculateHexagonDistance(v_TexCoord) * g_Resolution / 2.0;

    if (distance <= 0.0)
    {
        gl_FragColor.a = 0.0;
    }
    else
    {
        // blending range is implicitly 1.0.
        gl_FragColor.a *= distance;
    }
}