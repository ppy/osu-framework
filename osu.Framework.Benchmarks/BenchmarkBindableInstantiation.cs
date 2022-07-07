// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BenchmarkDotNet.Attributes;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkBindableInstantiation
    {
        [Benchmark]
        public Bindable<int> Instantiate() => new Bindable<int>();

        [Benchmark]
        public Bindable<int> GetBoundCopy() => new Bindable<int>().GetBoundCopy();

        [Benchmark(Baseline = true)]
        public Bindable<int> GetBoundCopyOld() => new BindableOld<int>().GetBoundCopy();

        private class BindableOld<T> : Bindable<T> where T : notnull
        {
            public BindableOld(T defaultValue = default!)
                : base(defaultValue)
            {
            }

            protected override Bindable<T> CreateInstance() => (BindableOld<T>)Activator.CreateInstance(GetType(), Value).AsNonNull();
        }
    }
}
