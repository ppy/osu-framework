// Automatically included for every vertex shader.

// The -1 is a placeholder value to offset all vertex input members
// of the actual vertex shader during inclusion of this header.
layout(location = -1) in highp float m_BackbufferDrawDepth;

void main()
{
    {{ real_main }}(); // Invoke real main func

    if (g_BackbufferDraw)
        gl_Position.z = m_BackbufferDrawDepth;

    if (g_IsDepthRangeZeroToOne)
        gl_Position.z = gl_Position.z / 2.0 + 0.5;

    if (g_IsClipSpaceYInverted)
        gl_Position.y = -gl_Position.y;
}