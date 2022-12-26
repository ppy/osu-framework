#include "sh_Utils.h"

varying highp vec2 uv;
uniform highp sampler2D tex;

void main()
{
    highp vec2 tex_coords = uv;
    gl_FragColor = toSRGB(texture2D(tex, tex_coords, 0.0));
}
