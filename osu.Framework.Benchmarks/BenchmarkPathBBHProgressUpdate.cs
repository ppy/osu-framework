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
        private readonly PathBBH bbh1000 = new PathBBH();
        private readonly PathBBH bbh10000 = new PathBBH();
        private readonly PathBBH bbh100000 = new PathBBH();
        private readonly PathBBH bbh1000000 = new PathBBH();

        private readonly Random random = new Random(1);

        public override void SetUp()
        {
            base.SetUp();

            List<Vector2> vertices100 = new List<Vector2>(100);
            List<Vector2> vertices1000 = new List<Vector2>(1000);
            List<Vector2> vertices10000 = new List<Vector2>(10000);
            List<Vector2> vertices100000 = new List<Vector2>(100000);
            List<Vector2> vertices1000000 = new List<Vector2>(1000000);

            for (int i = 0; i < 100; i++)
                vertices100.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            for (int i = 0; i < 1000; i++)
                vertices1000.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            for (int i = 0; i < 10000; i++)
                vertices10000.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            for (int i = 0; i < 100000; i++)
                vertices100000.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            for (int i = 0; i < 1000000; i++)
                vertices1000000.Add(new Vector2(random.NextSingle(), random.NextSingle()));

            bbh100.SetVertices(vertices100, 10);
            bbh1000.SetVertices(vertices1000, 10);
            bbh10000.SetVertices(vertices10000, 10);
            bbh100000.SetVertices(vertices100000, 10);
            bbh1000000.SetVertices(vertices1000000, 10);
        }

        [Benchmark]
        public void SetStartProgressBBH100()
        {
            bbh100.StartProgress = random.NextSingle();
        }

        [Benchmark]
        public void SetStartProgressBBH1000()
        {
            bbh1000.StartProgress = random.NextSingle();
        }

        [Benchmark]
        public void SetStartProgressBBH10000()
        {
            bbh10000.StartProgress = random.NextSingle();
        }

        [Benchmark]
        public void SetStartProgressBBH100000()
        {
            bbh100000.StartProgress = random.NextSingle();
        }

        [Benchmark]
        public void SetStartProgressBBH1000000()
        {
            bbh1000000.StartProgress = random.NextSingle();
        }
    }
}
