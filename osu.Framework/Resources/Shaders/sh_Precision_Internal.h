// This file is automatically included in every shader
#extension GL_ARB_uniform_buffer_object : require

#ifndef GL_ES
    #define lowp
    #define mediump
    #define highp
#else
    // GL_ES expects a defined precision for every member. Users may miss this requirement, so a default precision is specified.
    precision mediump float;
#endif
