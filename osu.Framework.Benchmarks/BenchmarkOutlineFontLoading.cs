// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp.Memory;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkOutlineFontLoading : BenchmarkTest
    {
        private NamespacedResourceStore<byte[]> baseResources = null!;

        public override void SetUp()
        {
            SixLabors.ImageSharp.Configuration.Default.MemoryAllocator = MemoryAllocator.Default;

            baseResources = new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.Benchmarks.dll"), @"Resources");
        }

        [Params(1, 10, 100, 1000, 10000)]
        public int FetchCount;

        private const string font_name = @"Fonts/FontAwesome5/FontAwesome-Solid";

        [Benchmark(Baseline = true)]
        public void BenchmarkNoCache()
        {
            using (var store = new OutlineGlyphStore(baseResources, font_name))
                runFor(store);
        }

        private void runFor(OutlineGlyphStore store)
        {
            store.LoadFontAsync().WaitSafely();

            var props = typeof(FontAwesome.Solid).GetProperties(BindingFlags.Public | BindingFlags.Static);

            int remainingCount = FetchCount;

            while (true)
            {
                foreach (var p in props)
                {
                    object? propValue = p.GetValue(null);
                    Debug.Assert(propValue != null);

                    var icon = (IconUsage)propValue;
                    using (var upload = store.Get(icon.Icon.ToString()))
                        Trace.Assert(upload.Data != null);

                    if (remainingCount-- == 0)
                        return;
                }
            }
        }
    }
}
