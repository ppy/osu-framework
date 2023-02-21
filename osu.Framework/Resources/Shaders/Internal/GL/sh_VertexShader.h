/**
* \brief Creates an "in" layout definition bound to the given location.
 * \param loc_num The location.
 */
#define IN(loc_num) attribute

/**
 * \brief Creates an "out" layout definition bound to the given location.
 * \param loc_num The location.
 */
#define OUT(loc_num) varying

vec4 SampleTexture(TEXTURE textureName, SAMPLER samplerName, vec2 coord)
{
    return texture2D(samplerName, coord);
}