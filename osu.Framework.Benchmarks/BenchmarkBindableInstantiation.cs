// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BenchmarkDotNet.Attributes;
using osu.Framework.Bindables;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkBindableInstantiation
    {
        [Benchmark(Baseline = true)]
        public Bindable<int> GetBoundCopyOld() => new BindableOld<int>().GetBoundCopy();

        [Benchmark]
        public Bindable<int> GetBoundCopy() => new Bindable<int>().GetBoundCopy();

        private class BindableOld<T> : Bindable<T>
        {
            public BindableOld(T defaultValue = default)
                : base(defaultValue)
            {
            }

            protected override Bindable<T> CreateInstance() => (BindableOld<T>)Activator.CreateInstance(GetType(), Value);
        }
    }
}
