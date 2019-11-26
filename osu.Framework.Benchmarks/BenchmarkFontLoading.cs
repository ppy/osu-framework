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

        private const string font_name = @"Fonts/FontAwesome5/FontAwesome-Solid";

        [Benchmark]
        public void BenchmarkRawCachingReuse()
        {
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

        //[Benchmark]
        public void BenchmarkSimple()
        {
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

            for (int i = 0; i < 10; i++)
            {
                foreach (var p in props)
                {
                    var icon = (IconUsage)p.GetValue(null);
                    using (var upload = store.Get(icon.Icon.ToString()))
                        Trace.Assert(upload.Data != null);
                }
            }
        }
    }
}
