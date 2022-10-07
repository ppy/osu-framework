#define PI 3.1415926536

bool insideProgressSector(highp vec2 pixelPos, mediump float progress, mediump float innerRadius, bool roundedCaps)
{
    if (progress >= 1.0)
        return true;

    // Compute angle of the current pixel in the (0, 2*PI) range
    mediump float pixelAngle = atan(0.5 - pixelPos.y, 0.5 - pixelPos.x) - PI / 2.0;
    if (pixelAngle < 0.0)
        pixelAngle += 2.0 * PI;

    mediump float progressAngle = 2.0 * PI * progress;

    if (pixelAngle < progressAngle)
        return true;

    if (!roundedCaps)
        return false;

    mediump float pathRadius = 0.25 * innerRadius;

    mediump vec2 arcStart = vec2(0.5, pathRadius);
    mediump vec2 arcEnd = vec2(0.5) + vec2(cos(progressAngle - PI / 2.0), sin(progressAngle - PI / 2.0)) * vec2(0.25 * (2.0 - innerRadius));

    mediump float dstToArc = min(distance(pixelPos, arcStart), distance(pixelPos, arcEnd));

    return dstToArc < pathRadius;
}

bool insideProgress(highp vec2 pixelPos, mediump float progress, mediump float innerRadius, bool roundedCaps)
{
    mediump float innerBorder = 0.5 * (1.0 - innerRadius);

    mediump float dstFromCentre = distance(pixelPos, vec2(0.5));
    bool insideRing = dstFromCentre > innerBorder && dstFromCentre < 0.5;

    return insideRing && insideProgressSector(pixelPos, progress, innerRadius, roundedCaps);
}
