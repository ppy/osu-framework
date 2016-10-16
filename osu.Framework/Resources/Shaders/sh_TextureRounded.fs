#ifdef GL_ES
    precision mediump float;
#endif

varying vec2 v_Position;
varying vec4 v_Colour;
varying vec2 v_TexCoord;

uniform sampler2D m_Sampler;

uniform float g_Radius;
uniform vec4 g_TexRect;
uniform vec2 g_TexSize;

uniform vec2 g_MaskingTopLeft;
uniform vec2 g_MaskingTopRight;
uniform vec2 g_MaskingBottomLeft;
uniform vec2 g_MaskingBottomRight;

#define EPSILON 0.001

// Returns whether pos is contained within a triangle spanned by the vertices p0,p1,p2
bool contains(vec2 pos, vec2 p0, vec2 p1, vec2 p2)
{
    // This code parametrizes pos as a linear combination of 2 edges s*(p1-p0) + t*(p2->p0).
    // pos is contained if s>0, t>0, s+t<1
    float area2 = (p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y - p1.y * p2.x);
    if (area2 == 0)
        return false;

    float s = (p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * pos.x + (p0.x - p2.x) * pos.y) / area2;
    if (s < -EPSILON)
        return false;

    float t = (p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * pos.x + (p1.x - p0.x) * pos.y) / area2;
    if (t < -EPSILON || (s + t) > 1+EPSILON)
        return false;

    return true;
}

// Returns whether pos is contained within the masking quad by checking 2 triangles making up
// the quad.
bool contains(vec2 pos)
{
    return
        contains(pos, g_MaskingBottomRight, g_MaskingBottomLeft, g_MaskingTopRight) ||
        contains(pos, g_MaskingTopLeft, g_MaskingTopRight, g_MaskingBottomLeft);
}

vec2 max3(vec2 a, vec2 b, vec2 c)
{
    return max(max(a, b), c);
}

float distanceFromRoundedRect()
{
    vec2 topLeftOffset = g_TexSize * (g_TexRect.xy - v_TexCoord);
    vec2 bottomRightOffset = g_TexSize * (v_TexCoord - g_TexRect.zw);

    vec2 distanceFromShrunkRect = max3(vec2(0.0), bottomRightOffset + vec2(g_Radius), topLeftOffset + vec2(g_Radius));
    return length(distanceFromShrunkRect);
}

void main(void)
{
    if (!contains(v_Position))
    {
        gl_FragColor = vec4(0.0);
        return;
    }

    float dist = g_Radius == 0.0 ? 0.0 : distanceFromRoundedRect();
    gl_FragColor = v_Colour * texture2D(m_Sampler, v_TexCoord, -0.9);

    // This correction is needed to avoid fading of the alpha value for radii below 1px.
    float radiusCorrection = max(0.0, 1.0 - g_Radius);
    if (dist > g_Radius - 1.0 + radiusCorrection)
        gl_FragColor.a *= max(0.0, g_Radius - dist + radiusCorrection);
}
