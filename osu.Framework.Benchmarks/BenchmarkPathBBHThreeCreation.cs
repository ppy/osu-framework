// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Lines;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkPathBBHThreeCreation : BenchmarkTest
    {
        private readonly List<Vector2> vertices100 = new List<Vector2>(100);
        private readonly List<Vector2> vertices1K = new List<Vector2>(1_000);
        private readonly List<Vector2> vertices10K = new List<Vector2>(10_000);
        private readonly List<Vector2> vertices100K = new List<Vector2>(100_000);
        private readonly List<Vector2> vertices1M = new List<Vector2>(1_000_000);

        private readonly PathBBH bbh = new PathBBH();

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
        public void CreateBBHWith100Leafs()
        {
            bbh.SetVertices(vertices100, 10);
        }

        [Benchmark]
        public void CreateBBHWith1KLeafs()
        {
            bbh.SetVertices(vertices1K, 10);
        }

        [Benchmark]
        public void CreateBBHWith10KLeafs()
        {
            bbh.SetVertices(vertices10K, 10);
        }

        [Benchmark]
        public void CreateBBHWith100KLeafs()
        {
            bbh.SetVertices(vertices100K, 10);
        }

        [Benchmark]
        public void CreateBBHWith1MLeafs()
        {
            bbh.SetVertices(vertices1M, 10);
        }
    }
}
