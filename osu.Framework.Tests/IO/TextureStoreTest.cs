// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TextureStoreTest
    {
        private TextureLoaderStore fontResourceStore = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            fontResourceStore = new TextureLoaderStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Drawable).Assembly), "Resources/Fonts"));
        }

        [Test]
        public void TestLookupStores()
        {
            using (var lookupStore1 = new NamespacedResourceStore<TextureUpload>(fontResourceStore, "Roboto"))
            using (var lookupStore2 = new NamespacedResourceStore<TextureUpload>(fontResourceStore, "RobotoCondensed"))
            using (var textureStore = new TextureStore(new DummyRenderer(), scaleAdjust: 100))
            {
                textureStore.AddTextureSource(lookupStore1);
                textureStore.AddTextureSource(lookupStore2);

                Assert.That(textureStore.GetAvailableResources().Contains("Roboto-Regular_0.png"));
                Assert.That(textureStore.GetStream("Roboto-Regular_0"), Is.Not.Null);

                var normalSheet = textureStore.Get("Roboto-Regular_0");
                Assert.That(normalSheet, Is.Not.Null);
                Assert.That(normalSheet.ScaleAdjust, Is.EqualTo(100));

                Assert.That(textureStore.GetAvailableResources().Contains("RobotoCondensed-Regular_0.png"));
                Assert.That(textureStore.GetStream("RobotoCondensed-Regular_0"), Is.Not.Null);

                var condensedSheet = textureStore.Get("RobotoCondensed-Regular_0");
                Assert.That(condensedSheet, Is.Not.Null);
                Assert.That(condensedSheet.ScaleAdjust, Is.EqualTo(100));
            }
        }

        [Test]
        public void TestNestedTextureStores()
        {
            using (var textureStore = new TextureStore(new DummyRenderer(), new NamespacedResourceStore<TextureUpload>(fontResourceStore, "Roboto"), scaleAdjust: 100))
            using (var nestedTextureStore = new TextureStore(new DummyRenderer(), new NamespacedResourceStore<TextureUpload>(fontResourceStore, "RobotoCondensed"), scaleAdjust: 200))
            {
                textureStore.AddStore(nestedTextureStore);

                Assert.That(textureStore.GetAvailableResources().Contains("Roboto-Regular_0.png"));
                Assert.That(textureStore.GetStream("Roboto-Regular_0"), Is.Not.Null);

                var normalSheet = textureStore.Get("Roboto-Regular_0");
                Assert.That(normalSheet, Is.Not.Null);
                Assert.That(normalSheet.ScaleAdjust, Is.EqualTo(100));

                Assert.That(textureStore.GetAvailableResources().Contains("RobotoCondensed-Regular_0.png"));
                Assert.That(textureStore.GetStream("RobotoCondensed-Regular_0"), Is.Not.Null);

                var condensedSheet = textureStore.Get("RobotoCondensed-Regular_0");
                Assert.That(condensedSheet, Is.Not.Null);
                Assert.That(condensedSheet.ScaleAdjust, Is.EqualTo(200));
            }
        }
    }
}
