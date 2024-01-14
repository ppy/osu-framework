// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Allocation;
using osu.Framework.Graphics;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public partial class BenchmarkLongRunningLoad
    {
        private Drawable nonLongRunningReflectionDrawable = null!;
        private Drawable longRunningReflectionDrawable = null!;
        private Drawable nonLongRunningSourceGenerationDrawable = null!;
        private Drawable longRunningSourceGenerationDrawable = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            nonLongRunningReflectionDrawable = new NonLongRunningReflectionDrawable();
            longRunningReflectionDrawable = new LongRunningReflectionDrawable();

            nonLongRunningSourceGenerationDrawable = new NonLongRunningSourceGenerationDrawable();
            longRunningSourceGenerationDrawable = new LongRunningSourceGenerationDrawable();
        }

        [Benchmark]
        public bool QueryNonLongRunningViaReflection() => nonLongRunningReflectionDrawable.IsLongRunning;

        [Benchmark]
        public bool QueryLongRunningViaReflection() => longRunningReflectionDrawable.IsLongRunning;

        [Benchmark]
        public bool QueryNonLongRunningViaSourceGeneration() => nonLongRunningSourceGenerationDrawable.IsLongRunning;

        [Benchmark]
        public bool QueryLongRunningViaSourceGeneration() => longRunningSourceGenerationDrawable.IsLongRunning;

#pragma warning disable OFSG001
        private class NonLongRunningReflectionDrawable : Drawable
        {
        }

        [LongRunningLoad]
        private class LongRunningReflectionDrawable : Drawable
        {
        }
#pragma warning restore OFSG001

        private partial class NonLongRunningSourceGenerationDrawable : Drawable
        {
        }

        [LongRunningLoad]
        private partial class LongRunningSourceGenerationDrawable : Drawable
        {
        }
    }
}
