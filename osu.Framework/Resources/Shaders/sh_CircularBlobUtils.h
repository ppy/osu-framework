#define HALF_PI 1.57079632679
#define TWO_PI 6.28318530718
#define SQRT3 1.732050808

// 2D noise and random https://thebookofshaders.com/11/

highp float random(highp vec2 st)
{
    return fract(sin(dot(floor(st), vec2(12.9898,78.233))) * 43758.5453123);
}

// we could use exact box calculation, however this implementation is way faster and good enough
highp bool insideBezierBoundingBox(highp vec2 point, highp vec2 p0, highp vec2 p1, highp vec2 p2, highp float inflation)
{
    highp vec2 topLeft = min(min(p0, p1), p2) - vec2(inflation);
    highp vec2 bottomRight = max(max(p0, p1), p2) + vec2(inflation);

    return point.x > topLeft.x && point.y > topLeft.y && point.x < bottomRight.x && point.y < bottomRight.y;
}

// Distance to Bezier curve https://www.shadertoy.com/view/MlKcDD

highp float dot2(highp vec2 v)
{
    return dot(v, v);
}

highp float dstToBezier(highp vec2 pos, highp vec2 A, highp vec2 B, highp vec2 C)
{
    highp vec2 a = B - A;
    highp vec2 b = A - 2.0 * B + C;
    highp vec2 c = a * 2.0;
    highp vec2 d = A - pos;

    highp float kk = 1.0 / dot2(b);
    highp float kx = kk * dot(a, b);
    highp float ky = kk * (2.0 * dot2(a) + dot(d, b)) / 3.0;
    
    highp float p = ky - kx * kx;
    highp float q = kx * (2.0 * kx * kx - 3.0 * ky) + kk * dot(d, a);
    highp float h = q * q + 4.0 * p * p * p;

    if (h >= 0.0) // 1 root
    {
        h = sqrt(h);
        highp vec2 x = (vec2(h, -h) - q) * 0.5;
        highp vec2 uv = sign(x) * pow(abs(x), vec2(1.0 / 3.0));
        highp float t = clamp(uv.x + uv.y - kx, 0.0, 1.0);
        return sqrt(dot2(d + (c + b * t) * t));
    }

    // 3 roots
    highp float z = sqrt(-p);
    highp float v = acos(q / (p * z * 2.0)) / 3.0;
    highp float m = cos(v);
    highp float n = sin(v) * SQRT3;
    highp vec2 t = clamp(vec2(m + m, -n - m) * z - kx, 0.0, 1.0);
    highp vec2 qx = d + (c + b * t.x) * t.x;
    highp vec2 qy = d + (c + b * t.y) * t.y;
    return sqrt(min(dot2(qx), dot2(qy)));
}

highp vec2 getVertexPosByAngle(mediump float angle, highp vec2 noisePosition, mediump float amplitude, mediump float pathRadius)
{
    highp vec2 cs = vec2(cos(angle), sin(angle));
    highp float vertexDstFromCentre = 0.5 * (1.0 - amplitude * random(noisePosition + cs * 20.0)) - pathRadius;
    return vec2(0.5) + cs * vertexDstFromCentre;
}

lowp float blobAlphaAt(highp vec2 pixelPos, mediump float innerRadius, highp float texelSize, int pointCount, mediump float amplitude, highp vec2 noisePosition)
{
    mediump float pathRadius = innerRadius * 0.25;
    highp float dst = 10.0;

    mediump float vertexAngleOffset = TWO_PI / float(pointCount);

    highp vec2 lastVertex = getVertexPosByAngle(-vertexAngleOffset, noisePosition, amplitude, pathRadius);
    highp vec2 currentVertex = getVertexPosByAngle(0.0, noisePosition, amplitude, pathRadius);
    highp vec2 midLeft = lerp(lastVertex, currentVertex, 0.5);

    for (int i = 0; i < pointCount; i++)
    {
        highp vec2 nextVertex = getVertexPosByAngle(float(i + 1) * vertexAngleOffset, noisePosition, amplitude, pathRadius);
        highp vec2 midRight = lerp(currentVertex, nextVertex, 0.5);

        if (insideBezierBoundingBox(pixelPos, midLeft, currentVertex, midRight, pathRadius))
        {
            dst = min(dst, dstToBezier(pixelPos, midLeft, currentVertex, midRight));

            if (dst < pathRadius - texelSize)
                return 1.0;
        }
        
        currentVertex = nextVertex;
        midLeft = midRight;
    }

    return smoothstep(texelSize, 0.0, dst - pathRadius + texelSize);
}
