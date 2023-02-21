// Automatically included for every fragment shader.

vec4 SampleTexture(TEXTURE textureName, SAMPLER samplerName, vec2 coord)
{
    return texture(samplerName, coord);
}

vec4 SampleTexture(TEXTURE textureName, SAMPLER samplerName, vec2 coord, float lodBias)
{
    return texture(samplerName, coord, lodBias);
}