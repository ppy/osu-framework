// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Desktop.Platform;

namespace osu.Framework.Desktop.Tests.Benchmark
{
    [TestFixture]
    public class BenchmarkTests
    {
        [Test]
        public void TestBenchmark()
        {
            using (var host = new HeadlessGameHost())
                host.Run(new VisualTests.Benchmark());
        }
    }
}