#define PI 3.1415926536
#define HALF_PI 1.57079632679
#define TWO_PI 6.28318530718

highp float dstToLine(highp vec2 start, highp vec2 end, highp vec2 pixelPos)
{
    highp float lineLength = distance(end, start);

    if (lineLength == 0.0)
        return distance(pixelPos, start);

    highp vec2 a = (end - start) / lineLength;
    highp vec2 closest = clamp(dot(a, pixelPos - start), 0.0, distance(end, start)) * a + start; // closest point on a line to from given position
    return distance(closest, pixelPos);
}

lowp float progressAlphaAt(highp vec2 pixelPos, mediump float progress, mediump float innerRadius, bool roundedCaps, highp float texelSize)
{
    // This is a bit of a hack to make progress appear smooth if it's radius < texelSize by making it more transparent while leaving thickness the same
    lowp float subAAMultiplier = 1.0;

    if (innerRadius < texelSize * 2.0)
    {
        subAAMultiplier = max(innerRadius / (texelSize * 2.0), 0.1);
        innerRadius = texelSize * 2.0;
    }

    // Compute angle of the current pixel in the (0, 2*PI) range
    mediump float pixelAngle = atan(0.5 - pixelPos.y, 0.5 - pixelPos.x) - HALF_PI;
    if (pixelAngle < 0.0)
        pixelAngle += TWO_PI;

    mediump float progressAngle = TWO_PI * progress;

    if (progress >= 1.0 || pixelAngle < progressAngle) // Pixel inside the sector
    {
        highp float dstFromCentre = distance(pixelPos, vec2(0.5));

        if (dstFromCentre > 0.5 - texelSize) // on the outer side of the ring
            return smoothstep(0.5, 0.5 - texelSize, dstFromCentre) * subAAMultiplier;

        highp float innerBorder = 0.5 * (1.0 - innerRadius);

        return smoothstep(innerBorder - texelSize, innerBorder, dstFromCentre) * subAAMultiplier;
    }

    progressAngle = progressAngle - HALF_PI;
    highp vec2 cs = vec2(cos(progressAngle), sin(progressAngle));

    if (roundedCaps) // Pixel outside the sector with rounded caps enabled
    {
        mediump float pathRadius = 0.25 * innerRadius;
        highp float halfTexel = texelSize * 0.5;

        highp vec2 arcStart = vec2(0.5, pathRadius + halfTexel);
        highp vec2 arcEnd = vec2(0.5) + cs * vec2(0.5 - pathRadius - halfTexel);

        highp float dstToArc = min(distance(pixelPos, arcStart), distance(pixelPos, arcEnd));

        return smoothstep(pathRadius + halfTexel, pathRadius - halfTexel, dstToArc) * subAAMultiplier;
    }

    highp float dstToIdleEdge = dstToLine(vec2(0.5, texelSize), vec2(0.5, 0.5 * innerRadius), pixelPos);

    highp vec2 rotatingEdgeTop = vec2(0.5) + cs * vec2(0.5 - texelSize);
    highp vec2 rotatingEdgeBottom = vec2(0.5) + cs * vec2(0.5 - 0.5 * innerRadius);
    highp float dstToRotatingEdge = dstToLine(rotatingEdgeTop, rotatingEdgeBottom, pixelPos);

    return smoothstep(texelSize, 0.0, min(dstToIdleEdge, dstToRotatingEdge)) * subAAMultiplier;
}
