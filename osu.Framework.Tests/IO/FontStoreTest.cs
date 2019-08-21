// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.IO.Stores;
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
            fontResourceStore = new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Drawable).Assembly.Location), "Resources.Fonts.OpenSans");

            storage.GetFullPath("./", true);
        }

        [Test]
        public void TestNestedScaleAdjust()
        {
            var fontStore = new FontStore(new GlyphStore(fontResourceStore, "OpenSans") { CacheStorage = storage }, scaleAdjust: 100);
            var nestedFontStore = new FontStore(new GlyphStore(fontResourceStore, "OpenSans-Bold") { CacheStorage = storage }, 10);

            fontStore.AddStore(nestedFontStore);

            var normalGlyph = (TexturedCharacterGlyph)fontStore.Get("OpenSans", 'a');
            var boldGlyph = (TexturedCharacterGlyph)fontStore.Get("OpenSans-Bold", 'a');

            Assert.That(normalGlyph.Scale, Is.EqualTo(1f / 100));
            Assert.That(boldGlyph.Scale, Is.EqualTo(1f / 10));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            storage.Dispose();
        }
    }
}
