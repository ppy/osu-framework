attribute highp vec2 pos;
varying highp vec2 uv;

void main()
{
    uv = pos;
    gl_Position = vec4(2.0 * pos - vec2(1.0), 0.0, 1.0);
}