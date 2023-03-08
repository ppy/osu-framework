// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkQuad
    {
        private Quad quad;

        [Params(10, 100, 1000)]
        public int NumPoints;

        private Vector2[] points = null!;
        private bool[] results = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            quad = new Quad(
                new Vector2(3, 0),
                new Vector2(5, 1),
                new Vector2(0, 5),
                new Vector2(7, 7)
            );

            points = new Vector2[NumPoints];
            results = new bool[NumPoints];

            var random = new Random(20230307);
            for (int i = 0; i < NumPoints; ++i)
                points[i] = new Vector2(random.Next(0, 10), random.Next(0, 10));
        }

        [Benchmark]
        public void Contains()
        {
            for (int i = 0; i < NumPoints; ++i)
                results[i] = quad.Contains(points[i]);
        }
    }
}
