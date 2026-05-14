#ifndef TRUE_PERSPECTIVE_FS
#define TRUE_PERSPECTIVE_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_TruePerspectiveParameters
{
    mediump float g_TopHeightScale;
    mediump float g_TopWidthScale;
    mediump float g_GlobalHorizontalScale;
    mediump float g_VerticalOffset;
};

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    highp float t_x = (v_TexCoord.x - v_TexRect.x) / v_TexRect.z;
    highp float t_y = (v_TexCoord.y - v_TexRect.y) / v_TexRect.w;

    highp float y_mapped = t_y - g_VerticalOffset;

    if (y_mapped < 0.0 || y_mapped > 1.0)
    {
        o_Colour = vec4(0.0);
        return;
    }

    highp float s = max(g_TopHeightScale, 0.001);
    highp float H_new = (1.0 + s) / 2.0;
    highp float y_src;

    if (abs(s - 1.0) < 0.001)
    {
        y_src = y_mapped;
    }
    else
    {
        highp float a = (1.0 - s) / 2.0;
        highp float b = s;
        highp float c = -y_mapped * H_new;
        highp float discriminant = b * b - 4.0 * a * c;

        if (discriminant < 0.0)
        {
            o_Colour = vec4(0.0);
            return;
        }

        y_src = (-b + sqrt(discriminant)) / (2.0 * a);
    }

    highp float perspectiveXScale = mix(g_TopWidthScale, 1.0, y_mapped);
    highp float totalXScale = perspectiveXScale * g_GlobalHorizontalScale;
    highp float localX_src = (t_x - 0.5) / totalXScale + 0.5;

    if (y_src < 0.0 || y_src > 1.0 || localX_src < 0.0 || localX_src > 1.0)
    {
        o_Colour = vec4(0.0);
        return;
    }

    highp vec2 sampleCoord = vec2(
        v_TexRect.x + localX_src * v_TexRect.z,
        v_TexRect.y + y_src * v_TexRect.w);

    vec2 wrappedCoord = wrap(sampleCoord, v_TexRect);
    o_Colour = getRoundedColor(wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9), wrappedCoord);
}

#endif
