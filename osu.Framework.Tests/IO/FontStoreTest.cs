// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using osu.Framework.Text;

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
        public void TestNestedScaleAdjust()
        {
            using (var fontStore = new FontStore(new DummyRenderer(), new RawCachingGlyphStore(fontResourceStore, "Roboto-Regular") { CacheStorage = storage }, scaleAdjust: 100))
            using (var nestedFontStore = new FontStore(new DummyRenderer(), new RawCachingGlyphStore(fontResourceStore, "Roboto-Bold") { CacheStorage = storage }, 10))
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
            using (var fontStore = new FontStore(new DummyRenderer(), glyphStore, 100))
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
