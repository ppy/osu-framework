// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NET6_0
using BenchmarkDotNet.Attributes;
using osu.Framework.Input.Handlers.Tablet;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkTabletDriver : BenchmarkTest
    {
        private TabletDriver driver = null!;

        public override void SetUp()
        {
            driver = TabletDriver.Create();
        }

        [Benchmark]
        public void DetectBenchmark()
        {
            driver.Detect();
        }
    }
}
#endif
