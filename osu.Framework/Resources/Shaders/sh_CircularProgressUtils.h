#define PI 3.1415926536

bool insideProgressSector(highp vec2 pixelPos, mediump float progress)
{
    if (progress >= 1.0)
        return true;

    mediump float angle = atan(0.5 - pixelPos.y, 0.5 - pixelPos.x) - PI / 2.0;

    if (angle < 0.0)
        angle += 2.0 * PI;

    return angle < 2.0 * PI * progress;
}

bool insideProgress(highp vec2 pixelPos, mediump float progress, mediump float innerRadius)
{
    mediump float innerBorder = 0.5 * (1.0 - innerRadius);

    mediump float dstFromCentre = distance(pixelPos, vec2(0.5));
    bool insideRing = dstFromCentre > innerBorder && dstFromCentre < 0.5;

    return insideRing && insideProgressSector(pixelPos, progress);
}
