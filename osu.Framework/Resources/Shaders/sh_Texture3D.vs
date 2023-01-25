#include "sh_Utils.h"

attribute highp vec3 m_Position;
attribute lowp vec4 m_Colour;
attribute highp vec2 m_TexCoord;

varying lowp vec4 v_Colour;
varying highp vec2 v_TexCoord;
varying highp vec4 v_TexRect;
varying mediump vec2 v_BlendRange;

uniform mat4 g_ProjMatrix;

#include "sh_Masking.vs.h"

void main(void)
{
    initMasking(m_Position.xy);

    v_Colour = m_Colour;
    v_TexCoord = m_TexCoord;
    v_TexRect = vec4(0.0);
    v_BlendRange = vec2(0.0);

    gl_Position = g_ProjMatrix * vec4(m_Position, 1.0);
}
