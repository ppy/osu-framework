// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osu.Framework.Tests;
using SixLabors.Memory;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkFontLoading : BenchmarkTest
    {
        private NamespacedResourceStore<byte[]> baseResources;
        private TemporaryNativeStorage sharedTemp;

        public override void SetUp()
        {
            SixLabors.ImageSharp.Configuration.Default.MemoryAllocator = ArrayPoolMemoryAllocator.CreateDefault();

            baseResources = new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources");
            sharedTemp = new TemporaryNativeStorage("fontstore-test" + Guid.NewGuid(), createIfEmpty: true);
        }

        [Params(1, 10, 100, 1000, 10000)]
        public int FetchCount;

        private const string font_name = @"Fonts/FontAwesome5/FontAwesome-Solid";

        [Benchmark]
        public void BenchmarkRawCachingReuse()
        {
            using (var store = new RawCachingGlyphStore(baseResources, font_name) { CacheStorage = sharedTemp })
                runFor(store);
        }

        [Benchmark(Baseline = true)]
        public void BenchmarkRawCaching()
        {
            using (var temp = new TemporaryNativeStorage("fontstore-test" + Guid.NewGuid(), createIfEmpty: true))
            using (var store = new RawCachingGlyphStore(baseResources, font_name) { CacheStorage = temp })
                runFor(store);
        }

        [Benchmark]
        public void BenchmarkNoCache()
        {
            if (FetchCount > 100) // gets too slow.
                throw new NotImplementedException();

            using (var store = new GlyphStore(baseResources, font_name))
                runFor(store);
        }

        [Benchmark]
        public void BenchmarkTimedExpiry()
        {
            SixLabors.ImageSharp.Configuration.Default.MemoryAllocator = ArrayPoolMemoryAllocator.CreateDefault();

            using (var store = new TimedExpiryGlyphStore(baseResources, font_name))
                runFor(store);
        }

        [Benchmark]
        public void BenchmarkTimedExpiryMemoryPooling()
        {
            using (var store = new TimedExpiryGlyphStore(baseResources, font_name))
                runFor(store);
        }

        private void runFor(GlyphStore store)
        {
            store.LoadFontAsync().Wait();

            var props = typeof(FontAwesome.Solid).GetProperties(BindingFlags.Public | BindingFlags.Static);

            int remainingCount = FetchCount;

            while (true)
            {
                foreach (var p in props)
                {
                    var icon = (IconUsage)p.GetValue(null);
                    using (var upload = store.Get(icon.Icon.ToString()))
                        Trace.Assert(upload.Data != null);

                    if (remainingCount-- == 0)
                        return;
                }
            }
        }
    }
}
