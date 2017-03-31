// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Desktop.Platform;
using osu.Framework.VisualTests;

namespace osu.Framework.Desktop.Tests.Benchmark
{
    [TestFixture]
    public class VisualTests
    {
        [Test]
        public void TestVisualTests()
        {
            using (var host = new HeadlessGameHost())
                host.Run(new AutomatedVisualTestGame());
        }
    }
}
