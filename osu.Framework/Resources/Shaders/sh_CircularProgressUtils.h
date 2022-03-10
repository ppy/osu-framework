#define PI 3.1415926536

bool insideProgressSector(vec2 pixelPos, float progress)
{
    if (progress >= 1.0)
        return true;

    float angle = atan(0.5 - pixelPos.y, 0.5 - pixelPos.x) - PI / 2.0;

    if (angle < 0.0)
        angle += 2.0 * PI;

    return angle < 2.0 * PI * progress;
}

bool insideProgress(vec2 pixelPos, float progress, float innerRadius)
{
    float innerBorder = 0.5 * (1.0 - innerRadius);
    float outerBorder = 0.5;

    float dstFromCentre = distance(pixelPos, vec2(0.5));
    bool insideRing = dstFromCentre > innerBorder && dstFromCentre < outerBorder;

    return insideRing && insideProgressSector(pixelPos, progress);
}
