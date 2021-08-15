// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BenchmarkDotNet.Attributes;
using osu.Framework.Bindables;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkBindableInstantiation
    {
        private Bindable<int> bindable;

        [GlobalSetup]
        public void GlobalSetup() => bindable = new Bindable<int>();

        /// <summary>
        /// Creates an instance of the bindable directly by construction.
        /// </summary>
        [Benchmark(Baseline = true)]
        public Bindable<int> CreateInstanceViaConstruction() => new Bindable<int>();

        /// <summary>
        /// Creates an instance of the bindable via <see cref="Activator.CreateInstance(Type, object[])"/>.
        /// This used to be how <see cref="Bindable{T}.GetBoundCopy"/> creates an instance before binding, which has turned out to be inefficient in performance.
        /// </summary>
        [Benchmark]
        public Bindable<int> CreateInstanceViaActivatorWithParams() => (Bindable<int>)Activator.CreateInstance(typeof(Bindable<int>), 0);

        /// <summary>
        /// Creates an instance of the bindable via <see cref="Activator.CreateInstance(Type)"/>.
        /// More performant than <see cref="CreateInstanceViaActivatorWithParams"/>, due to not passing parameters to <see cref="Activator"/> during instance creation.
        /// </summary>
        [Benchmark]
        public Bindable<int> CreateInstanceViaActivatorWithoutParams() => (Bindable<int>)Activator.CreateInstance(typeof(Bindable<int>), true);

        /// <summary>
        /// Creates an instance of the bindable via <see cref="IBindable.CreateInstance"/>.
        /// This is the current and most performant version used for <see cref="IBindable.GetBoundCopy"/>, as equally performant as <see cref="CreateInstanceViaConstruction"/>.
        /// </summary>
        [Benchmark]
        public Bindable<int> CreateInstanceViaBindableCreateInstance() => bindable.CreateInstance();
    }
}
