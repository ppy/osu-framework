#include "sh_Utils.h"
#include "sh_TextureWrapping.h"

varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;

uniform lowp sampler2D m_Sampler;

void main(void)
{
	gl_FragColor = toSRGB(v_Colour * wrappedSampler(wrap(v_TexCoord, v_TexRect), v_TexRect, m_Sampler, -0.9));
}
