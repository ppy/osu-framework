// Automatically included for every fragment shader.

/**
* \brief Creates an "in" layout definition bound to the given location.
 * \param loc_num The location.
 */
#define IN(loc_num) varying

/**
 * \brief Creates an "out" layout definition bound to the given location.
 * \param loc_num The location.
 */
#define OUT(loc_num)

vec4 SampleTexture(TEXTURE textureName, SAMPLER samplerName, vec2 coord)
{
    return texture2D(samplerName, coord);
}

vec4 SampleTexture(TEXTURE textureName, SAMPLER samplerName, vec2 coord, float lodBias)
{
    return texture2D(samplerName, coord, lodBias);
}