// Automatically included for every fragment shader.

void main()
{
    {{ real_main }}(); // Invoke real main func
    gl_FragColor = {{ temp_variable }};
}