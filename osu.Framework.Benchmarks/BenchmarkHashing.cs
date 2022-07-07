// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using osu.Framework.Extensions;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkHashing
    {
        private const string test_string = @"A string with reasonable length";
        private MemoryStream memoryStream = null!;

        [Benchmark]
        public string StringMD5() => test_string.ComputeMD5Hash();

        [Benchmark]
        public string StringSHA() => test_string.ComputeSHA2Hash();

        [GlobalSetup]
        public void GlobalSetup()
        {
            byte[] array = new byte[1024];
            var random = new Random(42);
            random.NextBytes(array);
            memoryStream = new MemoryStream(array);
        }

        [Benchmark]
        public string StreamMD5() => memoryStream.ComputeMD5Hash();

        [Benchmark]
        public string StreamSHA() => memoryStream.ComputeSHA2Hash();
    }
}
