#include "sh_Utils.h"

attribute highp vec3 m_Position;
attribute lowp vec4 m_Colour;
attribute mediump vec2 m_TexCoord;

varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;
varying mediump vec2 v_BlendRange;

uniform mat4 g_ProjMatrix;

#include "sh_Masking.vs.h"

void main(void)
{
    v_Colour = m_Colour;
    v_TexCoord = m_TexCoord;
    v_TexRect = vec4(0.0);
    v_BlendRange = vec2(0.0);

    gl_Position = g_ProjMatrix * vec4(m_Position, 1.0);

    initMasking(m_Position);
}
