#define PI 3.1415926536

bool insideProgress(highp vec2 pixelPos, mediump float progress, mediump float innerRadius, bool roundedCaps)
{
    // Compute angle of the current pixel in the (0, 2*PI) range
    mediump float pixelAngle = atan(0.5 - pixelPos.y, 0.5 - pixelPos.x) - PI / 2.0;
    if (pixelAngle < 0.0)
        pixelAngle += 2.0 * PI;

    mediump float progressAngle = 2.0 * PI * progress;

    if (progress >= 1.0 || pixelAngle < progressAngle) // Pixel inside the sector
    {
        highp float dstFromCentre = distance(pixelPos, vec2(0.5));
        highp float innerBorder = 0.5 * (1.0 - innerRadius);

        // Pixel within a ring
        return dstFromCentre < 0.5 && dstFromCentre > innerBorder;
    }

    if (roundedCaps) // Pixel outside the sector with rounded caps enabled
    {
        mediump float pathRadius = 0.25 * innerRadius;

        highp vec2 arcStart = vec2(0.5, pathRadius);
        highp vec2 arcEnd = vec2(0.5) + vec2(cos(progressAngle - PI / 2.0), sin(progressAngle - PI / 2.0)) * vec2(0.5 - pathRadius);

        highp float dstToArc = min(distance(pixelPos, arcStart), distance(pixelPos, arcEnd));

        // Pixel within a cap
        return dstToArc < pathRadius;
    }

    return false;
}
