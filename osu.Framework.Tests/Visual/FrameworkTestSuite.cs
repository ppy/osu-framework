// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public abstract class FrameworkTestSuite<T> : TestSuite<T> where T : TestScene, new()
    {
        protected override ITestSuiteTestRunner CreateRunner() => new FrameworkTestSuiteTestRunner();

        private class FrameworkTestSuiteTestRunner : TestSuiteTestRunner
        {
            [BackgroundDependencyLoader]
            private void load()
            {
                Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(Path.GetFileName(Assembly.GetExecutingAssembly().Location)), "Resources"));
            }
        }
    }
}
