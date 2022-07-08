// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using osu.Framework.Extensions;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkStreamExtensions
    {
        private MemoryStream memoryStream = null!;

        [Params(100, 10000, 1000000)]
        public int Length { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            byte[] array = new byte[Length];
            var random = new Random(42);
            random.NextBytes(array);
            memoryStream = new MemoryStream(array);
        }

        [Benchmark]
        public byte[] ReadAllBytesToArray()
        {
            return memoryStream.ReadAllBytesToArray();
        }
    }
}
