// Automatically included for every vertex shader.

// The -1 is a placeholder value to offset all vertex input members
// of the actual vertex shader during inclusion of this header.
layout(location = -1) in highp float m_BackbufferDrawDepth;

// Whether the backbuffer is currently being drawn to
uniform bool g_BackbufferDraw;

void main()
{
    {{ real_main }}(); // Invoke real main func

    if (g_BackbufferDraw)
        gl_Position.z = m_BackbufferDrawDepth;
}