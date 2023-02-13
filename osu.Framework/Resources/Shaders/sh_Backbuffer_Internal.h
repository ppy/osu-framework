// Automatically included for every vertex shader.

in highp float m_BackbufferDrawDepth;

void main()
{
    {{ real_main }}(); // Invoke real main func

    if (g_BackbufferDraw)
        gl_Position.z = m_BackbufferDrawDepth;
}