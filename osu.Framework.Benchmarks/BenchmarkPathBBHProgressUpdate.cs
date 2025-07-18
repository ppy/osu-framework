// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Lines;
using osuTK;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkPathBBHProgressUpdate : BenchmarkTest
    {
        private readonly PathBBH bbh100 = new PathBBH();
        private readonly PathBBH bbh1K = new PathBBH();
        private readonly PathBBH bbh10K = new PathBBH();
        private readonly PathBBH bbh100K = new PathBBH();
        private readonly PathBBH bbh1M = new PathBBH();

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
                vertices100.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            for (int i = 0; i < vertices1K.Capacity; i++)
                vertices1K.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            for (int i = 0; i < vertices10K.Capacity; i++)
                vertices10K.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            for (int i = 0; i < vertices100K.Capacity; i++)
                vertices100K.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            for (int i = 0; i < vertices1M.Capacity; i++)
                vertices1M.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            bbh100.SetVertices(vertices100, 10);
            bbh1K.SetVertices(vertices1K, 10);
            bbh10K.SetVertices(vertices10K, 10);
            bbh100K.SetVertices(vertices100K, 10);
            bbh1M.SetVertices(vertices1M, 10);
        }

        [Benchmark]
        public void SetStartProgressBBH100()
        {
            bbh100.StartProgress = random.NextSingle();
        }

        [Benchmark]
        public void SetStartProgressBBH1K()
        {
            bbh1K.StartProgress = random.NextSingle();
        }

        [Benchmark]
        public void SetStartProgressBBH10K()
        {
            bbh10K.StartProgress = random.NextSingle();
        }

        [Benchmark]
        public void SetStartProgressBBH100K()
        {
            bbh100K.StartProgress = random.NextSingle();
        }

        [Benchmark]
        public void SetStartProgressBBH1M()
        {
            bbh1M.StartProgress = random.NextSingle();
        }
    }
}
