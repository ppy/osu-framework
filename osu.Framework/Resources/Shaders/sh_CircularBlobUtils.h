#define HALF_PI 1.57079632679
#define TWO_PI 6.28318530718
#define SQRT3 1.732050808

// 2D noise and random https://thebookofshaders.com/11/

highp float random(highp vec2 st)
{
    return fract(sin(dot(floor(st), vec2(12.9898,78.233))) * 43758.5453123);
}

// we could use exact box calculation, however this implementation is way faster but still good enough
highp bool inBoundingBox(highp vec2 point, highp vec2 p0, highp vec2 p1, highp vec2 p2, highp float inflation)
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

highp float det(highp vec2 a, highp vec2 b)
{
    return a.x * b.y - a.y * b.x;
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
        highp vec2 q1 = d + (c + b * t) * t;
        return sqrt(dot2(q1)) * sign(det(c + 2.0 * b * t, q1));
    }
    
    // 3 roots
    highp float z = sqrt(-p);
    highp float v = acos(q / (p * z * 2.0)) / 3.0;
    highp float m = cos(v);
    highp float n = sin(v) * SQRT3;
    highp vec3 t = clamp(vec3(m + m, -n - m, n - m) * z - kx, 0.0, 1.0);
    highp vec2 qx = d + (c + b * t.x) * t.x;
    highp vec2 qy = d + (c + b * t.y) * t.y;
    highp vec2 qz = d + (c + b * t.z) * t.z;
    highp float dx = dot2(qx);
    highp float dy = dot2(qy);
    highp float dz = dot2(qz);
    highp float sx = det(c + 2.0 * b * t.x, qx);
    highp float sy = det(c + 2.0 * b * t.y, qy);
    highp float sz = det(c + 2.0 * b * t.z, qz);

    return sqrt(min(dx, dy)) * sign(dx < dy ? (dx < dz ? sx : sz) : sy);
}

highp vec2 getVertexPosByAngle(mediump float angle, highp vec2 noisePosition, mediump float amplitude, highp float texelSize)
{
    highp vec2 cs = vec2(cos(angle), sin(angle));
    highp float vertexDstFromCentre = 0.5 * (1.0 - amplitude * random(noisePosition + cs * 20.0)) * (1.0 - texelSize);
    return vec2(0.5) + cs * vertexDstFromCentre;
}

lowp float blobAlphaAt(highp vec2 pixelPos, mediump float pathRadius, highp float texelSize, int pointCount, mediump float amplitude, highp vec2 noisePosition)
{
    // distances to the closest curve
    highp float absDst = 10.0;
    highp float signedDst = -10.0;

    mediump float vertexAngleOffset = TWO_PI / float(pointCount);

    highp vec2 lastVertex = getVertexPosByAngle(-vertexAngleOffset, noisePosition, amplitude, texelSize);
    highp vec2 currentVertex = getVertexPosByAngle(0.0, noisePosition, amplitude, texelSize);
    highp vec2 curveStart = lerp(lastVertex, currentVertex, 0.5);

    for (int i = 0; i < pointCount; i++)
    {
        highp vec2 nextVertex = getVertexPosByAngle(float(i + 1) * vertexAngleOffset, noisePosition, amplitude, texelSize);
        highp vec2 curveEnd = lerp(currentVertex, nextVertex, 0.5);

        if (inBoundingBox(pixelPos, curveStart, currentVertex, curveEnd, pathRadius))
        {
            highp float dstToCurve = dstToBezier(pixelPos, curveStart, currentVertex, curveEnd);
            highp float absDstToCurve = abs(dstToCurve);

            // save distances to current curve if it's the closest one
            signedDst = mix(signedDst, dstToCurve, float(absDstToCurve < absDst));
            absDst = min(absDst, absDstToCurve);
        }
        
        currentVertex = nextVertex;
        curveStart = curveEnd;
    }

    return smoothstep(texelSize, 0.0, absDst + float(signedDst < 0.0) * (texelSize - pathRadius));
}
