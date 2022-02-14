// Automatically included for every vertex shader.

attribute highp float m_BackbufferDrawDepth;

// Whether the backbuffer is currently being drawn to
uniform bool g_BackbufferDraw;

void main()
{
    {{ real_main }}(); // Invoke real main func

    if (g_BackbufferDraw)
        gl_Position.z = m_BackbufferDrawDepth;
}