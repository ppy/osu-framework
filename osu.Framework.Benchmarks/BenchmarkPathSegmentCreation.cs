// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public partial class BenchmarkPathSegmentCreation : BenchmarkTest
    {
        private readonly List<Vector2> vertices100 = new List<Vector2>(100);
        private readonly List<Vector2> vertices1K = new List<Vector2>(1_000);
        private readonly List<Vector2> vertices10K = new List<Vector2>(10_000);
        private readonly List<Vector2> vertices100K = new List<Vector2>(100_000);
        private readonly List<Vector2> vertices1M = new List<Vector2>(1_000_000);

        private readonly BenchPath path = new BenchPath();
        private readonly Consumer consumer = new Consumer();

        public override void SetUp()
        {
            base.SetUp();

            var rng = new Random(1);

            for (int i = 0; i < vertices100.Capacity; i++)
                vertices100.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));

            for (int i = 0; i < vertices1K.Capacity; i++)
                vertices1K.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));

            for (int i = 0; i < vertices10K.Capacity; i++)
                vertices10K.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));

            for (int i = 0; i < vertices100K.Capacity; i++)
                vertices100K.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));

            for (int i = 0; i < vertices1M.Capacity; i++)
                vertices1M.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));
        }

        [Benchmark]
        public void Compute100Segments()
        {
            path.Vertices = vertices100;
            consumer.Consume(path.Segments);
        }

        [Benchmark]
        public void Compute1KSegments()
        {
            path.Vertices = vertices1K;
            consumer.Consume(path.Segments);
        }

        [Benchmark]
        public void Compute10KSegments()
        {
            path.Vertices = vertices10K;
            consumer.Consume(path.Segments);
        }

        [Benchmark]
        public void Compute100KSegments()
        {
            path.Vertices = vertices100K;
            consumer.Consume(path.Segments);
        }

        [Benchmark]
        public void Compute1MSegments()
        {
            path.Vertices = vertices1M;
            consumer.Consume(path.Segments);
        }

        private partial class BenchPath : Path
        {
            public IEnumerable<Line> Segments => BBH.Segments;
        }
    }
}
