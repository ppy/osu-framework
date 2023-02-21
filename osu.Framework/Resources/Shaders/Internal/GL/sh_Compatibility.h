// This file is automatically included in every shader.

#extension GL_ARB_uniform_buffer_object : enable

#define TEXTURE float

#define SAMPLER sampler2D

/**
* \brief Retrieves the set number for a uniform layout, offset by any internal framework layouts.
 * \param a The desired set number.
 */
#define BASE_SET_OFFSET(a) (a + 1)

/**
 * \brief Creates a uniform layout definition bound to binding 0 in the given set.
 *  This calls BASE_SET_OFFSET(set_num) internally.
 * \param set_num The desired set number.
 */
#define UNIFORM_BLOCK(set_num, uniform_name) layout(std140) uniform uniform_name

/**
 * \brief Creates a uniform texture layout definition bound to the given set.
 *  This calls BASE_SET_OFFSET(set_num) internally.
 * \param set_num The desired set number.
 */
#define UNIFORM_TEXTURE(set_num, texture_name, sampler_name) \
uniform lowp TEXTURE texture_name; \
uniform lowp SAMPLER sampler_name