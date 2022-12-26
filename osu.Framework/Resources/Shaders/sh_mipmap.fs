#include "sh_Utils.h"

varying highp vec2 uv;
uniform lowp sampler2D tex;

void main()
{
    gl_FragColor = toSRGB(texture2D(tex, uv, 0.0));
}
