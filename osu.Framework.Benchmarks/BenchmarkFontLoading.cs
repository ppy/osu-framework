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
        private static NamespacedResourceStore<byte[]> baseResources;

        public override void SetUp()
        {
            baseResources = new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.dll"), @"Resources");
        }

        private static TemporaryNativeStorage sharedTemp;

        private const string font_name = @"Fonts/FontAwesome5/FontAwesome-Solid";

        [Benchmark]
        public void BenchmarkRawCachingReuse()
        {
            sharedTemp ??= new TemporaryNativeStorage("fontstore-test" + Guid.NewGuid(), createIfEmpty: true);

            using (var store = new RawCachingGlyphStore(baseResources, font_name) { CacheStorage = sharedTemp })
                runFor(store);
        }

        [Benchmark]
        public void BenchmarkRawCaching()
        {
            using (var temp = new TemporaryNativeStorage("fontstore-test" + Guid.NewGuid(), createIfEmpty: true))
            using (var store = new RawCachingGlyphStore(baseResources, font_name) { CacheStorage = temp })
                runFor(store);
        }

        [Benchmark]
        public void BenchmarkSimple()
        {
            using (var store = new GlyphStore(baseResources, font_name))
                runFor(store);
        }

        [Benchmark]
        public void BenchmarkTimedExpiry()
        {
            SixLabors.ImageSharp.Configuration.Default.MemoryAllocator = ArrayPoolMemoryAllocator.CreateWithMinimalPooling();

            using (var store = new TimedExpiryGlyphStore(baseResources, font_name))
                runFor(store);
        }

        private void runFor(GlyphStore store)
        {
            store.LoadFontAsync().Wait();

            foreach (var p in typeof(FontAwesome.Solid).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                var icon = (IconUsage)p.GetValue(null);
                using (var upload = store.Get(icon.Icon.ToString()))
                    Trace.Assert(upload.Data != null);
            }
        }
    }
}
