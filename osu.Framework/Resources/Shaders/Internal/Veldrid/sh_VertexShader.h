vec4 SampleTexture(TEXTURE_TYPE textureName, SAMPLER_TYPE samplerName, vec2 coord)
{
    return texture(sampler2D(textureName, samplerName), coord);
}