// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Lines;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkPathReceivePositionalInputAt : BenchmarkTest
    {
        // We deliberately set path radius to 0 to introduce worst-case scenario in which with any position given we won't land on a path.
        private readonly Path path100 = new Path { PathRadius = 0f };
        private readonly Path path1K = new Path { PathRadius = 0f };
        private readonly Path path10K = new Path { PathRadius = 0f };
        private readonly Path path100K = new Path { PathRadius = 0f };
        private readonly Path path1M = new Path { PathRadius = 0f };

        private readonly Random random = new Random(1);

        public override void SetUp()
        {
            base.SetUp();

            List<Vector2> vertices100 = new List<Vector2>(100);
            List<Vector2> vertices1K = new List<Vector2>(1_000);
            List<Vector2> vertices10K = new List<Vector2>(10_000);
            List<Vector2> vertices100K = new List<Vector2>(100_000);
            List<Vector2> vertices1M = new List<Vector2>(1_000_000);

            for (int i = 0; i < vertices100.Capacity; i++)
                vertices100.Add(new Vector2((float)i / vertices100.Capacity * 100, random.NextSingle() * 100));

            for (int i = 0; i < vertices1K.Capacity; i++)
                vertices1K.Add(new Vector2((float)i / vertices1K.Capacity * 100, random.NextSingle() * 100));

            for (int i = 0; i < vertices10K.Capacity; i++)
                vertices10K.Add(new Vector2((float)i / vertices10K.Capacity * 100, random.NextSingle() * 100));

            for (int i = 0; i < vertices100K.Capacity; i++)
                vertices100K.Add(new Vector2((float)i / vertices100K.Capacity * 100, random.NextSingle() * 100));

            for (int i = 0; i < vertices1M.Capacity; i++)
                vertices1M.Add(new Vector2((float)i / vertices1M.Capacity * 100, random.NextSingle() * 100));

            path100.Vertices = vertices100;
            path1K.Vertices = vertices1K;
            path10K.Vertices = vertices10K;
            path100K.Vertices = vertices100K;
            path1M.Vertices = vertices1M;
        }

        [Benchmark]
        public void Contains100()
        {
            path100.ReceivePositionalInputAt(new Vector2(random.NextSingle() * 100, random.NextSingle() * 100));
        }

        [Benchmark]
        public void Contains1K()
        {
            path1K.ReceivePositionalInputAt(new Vector2(random.NextSingle() * 100, random.NextSingle() * 100));
        }

        [Benchmark]
        public void Contains10K()
        {
            path10K.ReceivePositionalInputAt(new Vector2(random.NextSingle() * 100, random.NextSingle() * 100));
        }

        [Benchmark]
        public void Contains100K()
        {
            path100K.ReceivePositionalInputAt(new Vector2(random.NextSingle() * 100, random.NextSingle() * 100));
        }

        [Benchmark]
        public void Contains1M()
        {
            path1M.ReceivePositionalInputAt(new Vector2(random.NextSingle() * 100, random.NextSingle() * 100));
        }
    }
}
