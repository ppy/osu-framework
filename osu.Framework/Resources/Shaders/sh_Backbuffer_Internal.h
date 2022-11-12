// Automatically included for every vertex shader.

attribute highp float m_BackbufferDrawDepth;
attribute highp vec2 m_MaskingTexCoord;

varying highp vec2 v_MaskingTexCoord;

// Whether the backbuffer is currently being drawn to
uniform bool g_BackbufferDraw;

void main()
{
    v_MaskingTexCoord = m_MaskingTexCoord;

    {{ real_main }}(); // Invoke real main func

    if (g_BackbufferDraw)
        gl_Position.z = m_BackbufferDrawDepth;
}