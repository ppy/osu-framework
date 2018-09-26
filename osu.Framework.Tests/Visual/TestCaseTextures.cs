// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseTextures : TestCase
    {
        [Cached]
        private readonly TextureStore normalStore = new TextureStore(new TextureLoaderStore(new OnlineStore()));

        [Cached]
        private readonly LargeTextureStore largeStore = new LargeTextureStore(new TextureLoaderStore(new OnlineStore()));

        /// <summary>
        /// Tests that a ref-counted texture is disposed when all references are lost.
        /// </summary>
        [Test]
        public void TestRefCountTextureDisposal()
        {
            Avatar avatar1 = null;
            Avatar avatar2 = null;
            TextureWithRefCount texture = null;

            AddStep("add disposable sprite", () => avatar1 = addSprite("https://a.ppy.sh/3"));
            AddStep("add disposable sprite", () => avatar2 = addSprite("https://a.ppy.sh/3"));

            AddUntilStep(() => (texture = (TextureWithRefCount)avatar1.Texture) != null && avatar2.Texture != null, "wait for texture load");

            AddAssert("textures share gl texture", () => avatar1.Texture.TextureGL == avatar2.Texture.TextureGL);
            AddAssert("textures have different refcount textures", () => avatar1.Texture != avatar2.Texture);

            AddStep("remove delayed from children", Clear);

            AddUntilStep(() => texture.ReferenceCount == 0, "gl textures disposed");
        }

        /// <summary>
        /// Tests that a ref-counted texture gets put in a non-available state when disposed.
        /// </summary>
        [Test]
        public void TestRefCountTextureAvailability()
        {
            Texture texture = null;

            AddStep("get texture", () => texture = largeStore.Get("https://a.ppy.sh/3"));
            AddStep("dispose texture", () => texture.Dispose());

            AddAssert("texture is not available", () => !texture.Available);
        }

        /// <summary>
        /// Tests that a non-ref-counted texture remains in an available state when disposed.
        /// </summary>
        [Test]
        public void TestTextureAvailability()
        {
            Texture texture = null;

            AddStep("get texture", () => texture = normalStore.Get("https://a.ppy.sh/3"));
            AddStep("dispose texture", () => texture.Dispose());

            AddAssert("texture is still available", () => texture.Available);
        }

        private Avatar addSprite(string url)
        {
            var avatar = new Avatar(url);
            Add(new DelayedLoadWrapper(avatar));
            return avatar;
        }

        private class Avatar : Sprite
        {
            private readonly string url;

            public Avatar(string url)
            {
                this.url = url;
            }

            [BackgroundDependencyLoader]
            private void load(LargeTextureStore textures)
            {
                Texture = textures.Get(url);
            }
        }
    }
}
