layout(location = 0) in vec2 v_MaskingPosition;
layout(location = 1) in vec4 v_Colour;
layout(location = 2) in vec2 v_TexCoord;
layout(location = 3) in vec4 v_TexRect;
layout(location = 4) in vec2 v_BlendRange;

layout(set = 1, binding = 0) uniform texture2D m_Texture;
layout(set = 1, binding = 1) uniform sampler m_Sampler;

//Width to sample from
uniform float mag;

//Alpha value
uniform float alpha;

uniform float redtint;

//Operate on a high range (0.5 - 1.0) or the full range (0.0 - 1.0)
uniform bool hirange;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    vec4 sum = pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord), vec4(2.0));

    //Accumulate the colour from 12 neighbouring pixels
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(-0.326212, -0.405805) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(-0.840144, -0.073580) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(-0.695914,  0.457137) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(-0.203345,  0.620716) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(0.962340, -0.194983) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(0.473434, -0.480026) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(0.519456,  0.767022) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(0.185461, -0.893124) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(0.507431,  0.064425) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(0.896420,  0.412458) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(-0.321940, -0.932615) * mag)), vec4(2.0));
    sum += pow(texture(sampler2D(m_Texture, m_Sampler), v_TexCoord + (vec2(-0.791559, -0.597705) * mag)), vec4(2.0));

    //Average the sum
    sum /= 13.0;
    sum = sqrt(sum);

    //Fix alpha
    sum.a *= alpha;

    //Expand the higher range if applicable
    if (hirange)
        sum.rgb = (sum.rgb - 0.5) * 2.0;

    sum.r += redtint;

	o_Colour = v_Colour * sum;
}