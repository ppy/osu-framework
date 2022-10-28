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

lowp float blobAlphaAt(highp vec2 pixelPos, mediump float innerRadius, highp float texelSize, mediump float frequency, mediump float amplitude, int seed)
{    
    // Compute angle of the current pixel in the (0, 2*PI) range
    mediump float pixelAngle = atan(0.5 - pixelPos.y, 0.5 - pixelPos.x) - HALF_PI;
    if (pixelAngle < 0.0)
        pixelAngle += TWO_PI;

    const int pointCount = 50;

    mediump float pathRadius = innerRadius * 0.25;

    highp float shortestDistance = 1.0;

    mediump float startAngle = pixelAngle - HALF_PI * 0.5 - HALF_PI;

    // Looks to be random enough but still consistent
    highp vec2 noisePosition = vec2(random(vec2(float(seed) * 0.0001)) * 100000.0, random(vec2(float(seed) * 0.0001)) * 100000.0);

    // Distance approximation
    // Plot points within a search range (90 degrees seems enough) and check which one is closest
    for (int i = 0; i < pointCount; i++)
    {
        mediump float angle = startAngle + HALF_PI * float(i) / float(pointCount);
        highp vec2 cs = vec2(cos(angle), sin(angle));

        highp float noiseValue = noise(noisePosition + cs * vec2(frequency));
        highp vec2 pos = vec2(0.5) + cs * vec2(0.5 - pathRadius - texelSize - noiseValue * 0.5 * amplitude);

        shortestDistance = min(shortestDistance, distance(pixelPos, pos));

        // No need to loop further if already inside the shape
        if (shortestDistance < pathRadius)
            break;
    }
    
    return smoothstep(texelSize, 0.0, shortestDistance - pathRadius);
}
