#ifndef SSBO_TEST_FS
#define SSBO_TEST_FS

#extension GL_ARB_shader_storage_buffer_object : enable

struct ColourData
{
    vec4 Colour;
};

#ifndef OSU_GRAPHICS_NO_SSBO

layout(std140, set = 0, binding = 0) readonly buffer g_ColourBuffer
{
    ColourData Data[];
} ColourBuffer;

#else // OSU_GRAPHICS_NO_SSBO

layout(std140, set = 0, binding = 0) uniform g_ColourBuffer
{
    ColourData Data[64];
} ColourBuffer;

#endif // OSU_GRAPHICS_NO_SSBO

layout(location = 0) flat in int v_ColourIndex;
layout(location = 0) out vec4 o_Colour;

void main(void)
{
    o_Colour = ColourBuffer.Data[v_ColourIndex].Colour;
}

#endif // SSBO_TEST_FS
