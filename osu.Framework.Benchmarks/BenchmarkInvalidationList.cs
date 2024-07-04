// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics;
using osu.Framework.Layout;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkInvalidationList
    {
        private InvalidationList list;

        [Benchmark]
        public void Invalidate() => list.Invalidate(InvalidationSource.Self, Invalidation.All);

        [Benchmark]
        public void Validate() => list.Validate(Invalidation.All);
    }
}
