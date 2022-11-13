#include "sh_Utils.h"

// Vertex inputs.
attribute highp vec2 m_Position;
attribute lowp vec4 m_Colour;
attribute mediump vec2 m_TexCoord;
attribute mediump vec4 m_TexRect;
attribute mediump vec2 m_BlendRange;

// Vertex outputs.
varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;
varying mediump vec2 v_BlendRange;

uniform highp mat4 g_ProjMatrix;

#include "sh_Masking.vs.h"

void main(void)
{
    v_Colour = m_Colour;
    v_TexCoord = m_TexCoord;
    v_TexRect = m_TexRect;
    v_BlendRange = m_BlendRange;

    gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);

    initMasking(vec3(m_Position, 1.0));
}
