layout(location = 0) in highp vec2 m_Position;
layout(location = 0) out highp vec2 v_TexCoord;

void main()
{
    v_TexCoord = m_Position;
    vec2 position = vec2(m_Position.x, 1.0 - m_Position.y);
    gl_Position = vec4(2.0 * position - vec2(1.0), 0.0, 1.0);
}