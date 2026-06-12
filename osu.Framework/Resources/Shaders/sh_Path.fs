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
    
    // in sh_PathPrepass.fs we were encoding [0, 1] 24-bit float as 3 8-bit floats. Decoding to get original value
    highp float r = frameBuffer.r * 255.0;
    highp float g = frameBuffer.g * 255.0;
    highp float b = frameBuffer.b * 255.0;
    highp float v = r * 65536.0 + g * 256.0 + b;
    highp float decodedDst = v / 16777215.0;

    lowp vec4 pathCol = texture(sampler2D(m_Texture1, m_Sampler1), TexRect1.xy + vec2(decodedDst, 0.0) * TexRect1.zw, -0.9);
    o_Colour = getRoundedColor(vec4(pathCol.rgb, pathCol.a * frameBuffer.a), v_TexCoord);
}

#endif
