/**
* \brief Creates an "in" layout definition bound to the given location.
 * \param loc_num The location.
 */
#define IN(loc_num) varying

vec4 SampleTexture(TEXTURE textureName, SAMPLER samplerName, vec2 coord)
{
    return texture2D(samplerName, coord);
}

vec4 SampleTexture(TEXTURE textureName, SAMPLER samplerName, vec2 coord, float lodBias)
{
    return texture2D(samplerName, coord, lodBias);
}