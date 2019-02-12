// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public abstract class FrameworkTestCase : TestCase
    {
        protected override ITestCaseTestRunner CreateRunner() => new FrameworkTestCaseTestRunner();

        private class FrameworkTestCaseTestRunner : TestCaseTestRunner
        {
            [BackgroundDependencyLoader]
            private void load()
            {
                Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(Path.GetFileName(Assembly.GetExecutingAssembly().Location)), "Resources"));
            }
        }
    }
}
