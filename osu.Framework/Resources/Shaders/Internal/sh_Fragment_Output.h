// Automatically included for every fragment shader.

{{ fragment_output_layout }}

void main()
{
    {{ real_main }}(); // Invoke real main func

    // Ensure no fragment input is culled out from the buffer.
    {{ fragment_output_assignment }}
}