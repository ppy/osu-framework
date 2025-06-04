// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using osu.Framework.Platform;
using osu.Framework.Platform.Linux.Native;
using osu.Framework.Platform.Windows.Native;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkSleep : BenchmarkTest
    {
        private INativeSleep nativeSleep = null!;

        private readonly TimeSpan timeSpan = TimeSpan.FromMilliseconds(1.5);

        public override void SetUp()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
                nativeSleep = new WindowsNativeSleep();
            else if (RuntimeInfo.IsUnix && UnixNativeSleep.Available)
                nativeSleep = new UnixNativeSleep();
        }

        [Benchmark]
        public void TestThreadSleep()
        {
            Thread.Sleep(timeSpan);
        }

        [Benchmark]
        public void TestNativeSleep()
        {
            nativeSleep.Sleep(timeSpan);
        }
    }
}
