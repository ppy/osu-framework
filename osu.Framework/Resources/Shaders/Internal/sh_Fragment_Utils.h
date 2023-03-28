vec4 sampleTexture(texture2D tex, sampler samp, AuxTextureData auxData, vec2 coord, float lodBias)
{
    vec4 col = texture(sampler2D(tex, samp), coord);

    if (auxData.IsFrameBufferTexture)
        col = toLinear(col);

    return col;
}

vec4 sampleTexture(texture2D tex, sampler samp, vec2 coord, float lodBias)
{
    AuxTextureData auxData;
    auxData.IsFrameBufferTexture = false;

    return sampleTexture(tex, samp, auxData, coord, lodBias);
}

vec4 sampleTexture(texture2D tex, sampler samp, AuxTextureData auxData, vec2 coord)
{
    return sampleTexture(tex, samp, auxData, coord, 0.0);
}

vec4 sampleTexture(texture2D tex, sampler samp, vec2 coord)
{
    return sampleTexture(tex, samp, coord, 0.0);
}