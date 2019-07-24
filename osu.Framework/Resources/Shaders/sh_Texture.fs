#include "sh_Utils.h"

varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;

void main(void)
{
	gl_FragColor = toSRGB(v_Colour * texture2D(m_Sampler, v_TexCoord, -0.9));
}
