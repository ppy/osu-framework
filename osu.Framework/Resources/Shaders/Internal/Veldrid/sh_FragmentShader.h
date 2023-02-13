vec4 SampleTexture(TEXTURE_TYPE textureName, SAMPLER_TYPE samplerName, vec2 coord)
{
    return texture(sampler2D(textureName, samplerName), coord);
}

vec4 SampleTexture(TEXTURE_TYPE textureName, SAMPLER_TYPE samplerName, vec2 coord, float lodBias)
{
    return texture(sampler2D(textureName, samplerName), coord, lodBias);
}