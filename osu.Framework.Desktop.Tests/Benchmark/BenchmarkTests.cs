using System;
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
            {
                host.Add(new osu.Framework.VisualTests.Benchmark());
                host.Run();
            }
        }
    }
}