// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using osu.Framework.Allocation;

#pragma warning disable IDE0052 // Unread private member

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

    [SuppressMessage("Performance", "OFSG001:Class contributes to dependency injection and should be partial")]
    public class ClassInjectedWithReflection : IDependencyInjectionCandidate
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [Resolved]
        private object dependency { get; set; } = null!;
    }

    // This inspection can be removed once the source generator is merged in/referenced as a package.
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class ClassInjectedWithSourceGenerator : IDependencyInjectionCandidate
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [Resolved]
        private object dependency { get; set; } = null!;
    }
}
