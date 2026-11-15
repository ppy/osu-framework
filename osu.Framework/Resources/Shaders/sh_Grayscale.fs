#ifndef GRAYSCALE_FS
#define GRAYSCALE_FS

#include "sh_Utils.h"

const mediump float p_r = 0.299f;
const mediump float p_g = 0.587f;
const mediump float p_b = 0.114f;

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_GrayscaleParameters
{
	mediump float g_Strength;
};

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
	vec4 colour = texture(sampler2D(m_Texture, m_Sampler), v_TexCoord);
	float gray = dot(colour.rgb, vec3(p_r, p_g, p_b));
	vec3 blend = mix(colour.rgb, vec3(gray), g_Strength);

	o_Colour = vec4(blend, colour.a);
}

#endif