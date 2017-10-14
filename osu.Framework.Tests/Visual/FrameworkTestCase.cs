// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class FrameworkTestCase : TestCase
    {
        public override void RunTest()
        {
            using (var host = new HeadlessGameHost())
                host.Run(new FrameworkTestCaseTestRunner(this));
        }

        private class FrameworkTestCaseTestRunner : TestCaseTestRunner
        {
            public FrameworkTestCaseTestRunner(TestCase testCase)
                : base(testCase)
            {
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.Tests.exe"), "Resources"));
            }
        }
    }
}
