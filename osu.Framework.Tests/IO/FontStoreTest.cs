// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using osu.Framework.Text;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class FontStoreTest
    {
        private ResourceStore<byte[]> fontResourceStore;
        private TemporaryNativeStorage storage;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            storage = new TemporaryNativeStorage("fontstore-test");
            fontResourceStore = new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Drawable).Assembly), "Resources.Fonts.Roboto");
        }

        [Test]
        public void TestRefetchOnCacheFailure()
        {
            using (var fontStore = new RawCachingGlyphStore(fontResourceStore, "Roboto-Regular") { CacheStorage = storage })
            {
                fontStore.LoadFontAsync();

                var upload = fontStore.Get("a");
                Assert.That(hasNonZeroContent(upload.Data), Is.True);
            }

            // first cached fetch
            using (var fontStore = new RawCachingGlyphStore(fontResourceStore, "Roboto-Regular") { CacheStorage = storage })
            {
                fontStore.LoadFontAsync();

                var upload = fontStore.Get("a");
                Assert.That(hasNonZeroContent(upload.Data), Is.True);
            }

            // intentionally corrupt files
            foreach (var f in storage.GetFiles(string.Empty))
            {
                using (var stream = storage.GetStream(f, FileAccess.Write))
                {
                    for (int i = 0; i < stream.Length; i++)
                        stream.WriteByte(0);
                }
            }

            // second cached fetch
            using (var fontStore = new RawCachingGlyphStore(fontResourceStore, "Roboto-Regular") { CacheStorage = storage })
            {
                fontStore.LoadFontAsync();

                var upload = fontStore.Get("a");
                Assert.That(hasNonZeroContent(upload.Data), Is.True);
            }
        }

        private bool hasNonZeroContent(ReadOnlySpan<Rgba32> uploadData)
        {
            for (int i = 0; i < uploadData.Length; i++)
            {
                var pixel = uploadData[i];
                if (pixel.A != 0)
                    return true;
            }

            return false;
        }

        [Test]
        public void TestNestedScaleAdjust()
        {
            using (var fontStore = new FontStore(new RawCachingGlyphStore(fontResourceStore, "Roboto-Regular") { CacheStorage = storage }, scaleAdjust: 100))
            using (var nestedFontStore = new FontStore(new RawCachingGlyphStore(fontResourceStore, "Roboto-Bold") { CacheStorage = storage }, 10))
            {
                fontStore.AddStore(nestedFontStore);

                var normalGlyph = (TexturedCharacterGlyph)fontStore.Get("Roboto-Regular", 'a');
                Assert.That(normalGlyph, Is.Not.Null);

                var boldGlyph = (TexturedCharacterGlyph)fontStore.Get("Roboto-Bold", 'a');
                Assert.That(boldGlyph, Is.Not.Null);

                Assert.That(normalGlyph.Scale, Is.EqualTo(1f / 100));
                Assert.That(boldGlyph.Scale, Is.EqualTo(1f / 10));
            }
        }

        [Test]
        public void TestNoCrashOnMissingResources()
        {
            using (var glyphStore = new RawCachingGlyphStore(fontResourceStore, "DoesntExist") { CacheStorage = storage })
            using (var fontStore = new FontStore(glyphStore, 100))
            {
                Assert.That(glyphStore.Get('a'), Is.Null);

                Assert.That(fontStore.Get("DoesntExist", 'a'), Is.Null);
                Assert.That(fontStore.Get("OtherAttempt", 'a'), Is.Null);
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            storage.Dispose();
        }
    }
}
