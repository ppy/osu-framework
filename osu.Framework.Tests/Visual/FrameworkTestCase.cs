// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public abstract class FrameworkTestCase : TestCase
    {
        private class FrameworkTestCaseTestRunner : TestCaseTestRunner
        {
            public FrameworkTestCaseTestRunner(TestCase testCase)
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
