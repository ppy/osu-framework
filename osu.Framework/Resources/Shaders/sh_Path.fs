#ifndef PATH_FS
#define PATH_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

// FrameBuffer texture
layout(set = 0, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 0, binding = 1) uniform lowp sampler m_Sampler;

// Path texture
layout(set = 1, binding = 0) uniform lowp texture2D m_Texture1;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler1;

layout(std140, set = 2, binding = 0) uniform m_PathTextureParameters
{
	highp vec4 TexRect1;
};

layout(location = 0) out vec4 o_Colour;

void main(void) 
{
    lowp vec4 frameBuffer = texture(sampler2D(m_Texture, m_Sampler), v_TexCoord, -0.9);
    if (frameBuffer.r == 0.0)
    {
        o_Colour = vec4(0.0);
        return;
    }
    lowp vec4 pathCol = texture(sampler2D(m_Texture1, m_Sampler1), TexRect1.xy + vec2(frameBuffer.r, 0.0) * TexRect1.zw, -0.9);
    o_Colour = getRoundedColor(vec4(pathCol.rgb, pathCol.a * frameBuffer.a), v_TexCoord);
}

#endif
