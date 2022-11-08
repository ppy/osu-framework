// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Allocation;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkDependencyInjection
    {
        [ParamsAllValues]
        public bool ClearCaches { get; set; }

        private DependencyContainer dependencyContainer = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            dependencyContainer = new DependencyContainer();
            dependencyContainer.Cache(new object());
        }

        [Benchmark]
        public void TestInjectWithReflection()
        {
            if (ClearCaches)
                DependencyActivator.ClearCache();
            DependencyActivator.Activate(new ClassInjectedWithReflection(), dependencyContainer);
        }

        [Benchmark]
        public void TestWithSourceGenerator()
        {
            if (ClearCaches)
                DependencyActivator.ClearCache();
            DependencyActivator.Activate(new ClassInjectedWithSourceGenerator(), dependencyContainer);
        }
    }

    public class ClassInjectedWithReflection
    {
        [Resolved]
        private object dependency { get; set; } = null!;
    }

    public partial class ClassInjectedWithSourceGenerator
    {
        [Resolved]
        private object dependency { get; set; } = null!;
    }
}
