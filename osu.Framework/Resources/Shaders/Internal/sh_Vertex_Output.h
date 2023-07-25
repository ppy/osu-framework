// Automatically included for every vertex shader.

#ifndef INTERNAL_VERTEX_OUTPUT_H
#define INTERNAL_VERTEX_OUTPUT_H

void main()
{
    {{ real_main }}(); // Invoke real main func

    if (g_IsDepthRangeZeroToOne)
        gl_Position.z = gl_Position.z / 2.0 + 0.5;

    // When the device's texture coordinates are inverted, and when we are outputting to a framebuffer,
    // we should ensure that the framebuffer output is also inverted so that it's treated as a normal texture
    // later on in the frame.
    bool requiresFramebufferInvert = !g_BackbufferDraw && !g_IsUvOriginTopLeft;

    if (g_IsClipSpaceYInverted || requiresFramebufferInvert)
        gl_Position.y = -gl_Position.y;
}

#endif