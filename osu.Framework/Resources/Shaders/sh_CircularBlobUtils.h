#define HALF_PI 1.57079632679
#define TWO_PI 6.28318530718

// 2D noise and random https://thebookofshaders.com/11/

highp float random(highp vec2 st)
{
    return fract(sin(dot(st.xy, vec2(12.9898,78.233))) * 43758.5453123);
}

highp float noise(highp vec2 st)
{
    vec2 i = floor(st);
    vec2 f = fract(st);

    highp float a = random(i);
    highp float b = random(i + vec2(1.0, 0.0));
    highp float c = random(i + vec2(0.0, 1.0));
    highp float d = random(i + vec2(1.0, 1.0));

    highp vec2 u = f * f * (3.0 - 2.0 * f);

    return mix(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

lowp float blobAlphaAt(highp vec2 pixelPos, mediump float innerRadius, highp float texelSize, mediump float frequency, mediump float amplitude, highp vec2 noisePosition)
{
    // Compute angle of the current pixel in the (0, 2*PI) range
    mediump float pixelAngle = atan(0.5 - pixelPos.y, 0.5 - pixelPos.x) - HALF_PI;
    if (pixelAngle < 0.0)
        pixelAngle += TWO_PI;

    mediump float complexity = (frequency + amplitude) * 0.5 + 1.0;

    int pointCount = int(ceil(5.0 * complexity));
    mediump float searchRange = 0.1 * complexity; // in radians

    mediump float pathRadius = innerRadius * 0.25;

    highp float shortestDistance = 1.0;

    mediump float startAngle = pixelAngle - searchRange * 0.5;

    // Path approximation
    // Plot points within a search range and check which one is closest
    for (int i = 0; i < pointCount; i++)
    {
        mediump float angle = startAngle + searchRange * float(i) / float(pointCount);
        highp vec2 cs = vec2(cos(angle - HALF_PI), sin(angle - HALF_PI));

        highp float noiseValue = noise(noisePosition + cs * vec2(frequency));
        highp vec2 pos = vec2(0.5) + cs * vec2(0.5 - pathRadius - texelSize - noiseValue * 0.5 * amplitude);

        shortestDistance = min(shortestDistance, distance(pixelPos, pos));
    }
    
    return smoothstep(texelSize, 0.0, shortestDistance - pathRadius);
}
