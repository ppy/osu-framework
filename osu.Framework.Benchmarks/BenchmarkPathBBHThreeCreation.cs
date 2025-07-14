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
        private readonly List<Vector2> vertices1000 = new List<Vector2>(1000);
        private readonly List<Vector2> vertices10000 = new List<Vector2>(10000);
        private readonly List<Vector2> vertices100000 = new List<Vector2>(100000);
        private readonly List<Vector2> vertices1000000 = new List<Vector2>(1000000);

        private readonly PathBBH bbh = new PathBBH();

        public override void SetUp()
        {
            base.SetUp();

            var rng = new Random(1);

            for (int i = 0; i < 100; i++)
                vertices100.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));

            for (int i = 0; i < 1000; i++)
                vertices1000.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));

            for (int i = 0; i < 10000; i++)
                vertices10000.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));

            for (int i = 0; i < 100000; i++)
                vertices100000.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));

            for (int i = 0; i < 1000000; i++)
                vertices1000000.Add(new Vector2(rng.NextSingle(), rng.NextSingle()));
        }

        [Benchmark]
        public void CreateBBHWith100Leafs()
        {
            bbh.SetVertices(vertices100, 10);
        }

        [Benchmark]
        public void CreateBBHWith1000Leafs()
        {
            bbh.SetVertices(vertices1000, 10);
        }

        [Benchmark]
        public void CreateBBHWith10000Leafs()
        {
            bbh.SetVertices(vertices10000, 10);
        }

        [Benchmark]
        public void CreateBBHWith100000Leafs()
        {
            bbh.SetVertices(vertices100000, 10);
        }

        [Benchmark]
        public void CreateBBHWith1000000Leafs()
        {
            bbh.SetVertices(vertices1000000, 10);
        }
    }
}
