#ifndef TEXTURE_CHANNELS_FS
#define TEXTURE_CHANNELS_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_TextureChannelParameters
{
    float channelValue;
};

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

void main(void) 
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    lowp vec4 col = wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9);

    // works in tandem with TexturePreviewDrawNode.getChannelFloatRepresentation
    if (channelValue > 1f)
    {
        if (channelValue > 4f) // alpha only mode
        {
            col = vec4(col.a, col.a, col.a, 1.0);
        }
        else
        {
            bool R = channelValue < 2f;
            bool G = channelValue > 2f && channelValue < 3f;
            bool B = channelValue > 3f;
            col *= vec4(float(R), float(G), float(B), 1.0);
        }
    }
    
    o_Colour = getRoundedColor(col, wrappedCoord);
}

#endif
