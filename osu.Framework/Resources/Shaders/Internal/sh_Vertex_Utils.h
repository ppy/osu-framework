vec4 sampleTexture(texture2D tex, sampler samp, AuxTextureData auxData, vec2 coord)
{
    vec4 col = texture(sampler2D(tex, samp), coord);

    if (auxData.IsFrameBufferTexture)
        col = toLinear(col);

    return col;
}

vec4 sampleTexture(texture2D tex, sampler samp, vec2 coord)
{
    AuxTextureData auxData;
    auxData.IsFrameBufferTexture = false;

    return sampleTexture(tex, samp, auxData, coord);
}