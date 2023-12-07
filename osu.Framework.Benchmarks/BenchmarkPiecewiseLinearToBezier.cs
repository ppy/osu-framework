// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkPiecewiseLinearToBezier : BenchmarkTest
    {
        private Vector2[] inputPath = null!;

        [Params(5, 25)]
        public int NumControlPoints;

        [Params(5, 200)]
        public int NumTestPoints;

        [Params(0, 100, 200)]
        public int MaxIterations;

        public override void SetUp()
        {
            base.SetUp();

            Vector2[] points = new Vector2[5];
            points[0] = new Vector2(50, 250);
            points[1] = new Vector2(150, 230);
            points[2] = new Vector2(100, 150);
            points[3] = new Vector2(200, 80);
            points[4] = new Vector2(250, 50);
            inputPath = PathApproximator.LagrangePolynomialToPiecewiseLinear(points).ToArray();
        }

        [Benchmark]
        public List<Vector2> PiecewiseLinearToBezier()
        {
            return PathApproximator.PiecewiseLinearToBezier(inputPath, NumControlPoints, NumTestPoints, MaxIterations);
        }
    }
}
