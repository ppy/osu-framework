// 0 -> None
// 1 -> Clamp
// 2 -> Repeat

uniform int g_WrapModeS;
uniform int g_WrapModeT;

float wrap(float coord, int mode, float rangeMin, float rangeMax)
{
    if (mode == 1)
    {
        return clamp(coord, rangeMin, rangeMax);
    }
    else if (mode == 2)
    {
        return mod(coord - rangeMin, rangeMax - rangeMin) + rangeMin;
    }
    
    return coord;
}

vec2 wrap(vec2 texCoord, vec4 texRect)
{
    return vec2(wrap(texCoord.x, g_WrapModeS, texRect[0], texRect[2]), wrap(texCoord.y, g_WrapModeT, texRect[1], texRect[3]));
}