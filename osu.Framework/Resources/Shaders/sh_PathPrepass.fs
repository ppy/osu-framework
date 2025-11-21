#ifndef PATH_PREPASS_FS
#define PATH_PREPASS_FS

layout(location = 0) in highp vec2 v_RelativePos;

layout(location = 0) out vec4 o_Colour;

void main(void) 
{    
    highp float dst = clamp(distance(v_RelativePos, vec2(0.0)), 0.0, 1.0);
    o_Colour = vec4(vec3(1.0 - dst), float(dst < 1.0));
}

#endif