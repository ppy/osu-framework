// Automatically included for every fragment shader.

#ifndef INTERNAL_FRAGMENT_OUTPUT_H
#define INTERNAL_FRAGMENT_OUTPUT_H

{{ fragment_output_layout }}

void main()
{
    {{ real_main }}(); // Invoke real main func

    // Ensure no fragment input is culled out from the shader by passing them in the output.
    {{ fragment_output_assignment }}
}

#endif