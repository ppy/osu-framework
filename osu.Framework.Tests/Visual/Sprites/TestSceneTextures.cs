// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneTextures : FrameworkTestScene
    {
        [Cached]
        private TextureStore normalStore;

        [Cached]
        private LargeTextureStore largeStore;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var host = parent.Get<GameHost>();

            normalStore = new TextureStore(host.CreateTextureLoaderStore(new OnlineStore()));
            largeStore = new LargeTextureStore(host.CreateTextureLoaderStore(new OnlineStore()));

            return base.CreateChildDependencies(parent);
        }

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

            AddUntilStep("wait for texture load", () => (texture = (TextureWithRefCount)avatar1.Texture) != null && avatar2.Texture != null);

            AddAssert("textures share gl texture", () => avatar1.Texture.TextureGL == avatar2.Texture.TextureGL);
            AddAssert("textures have different refcount textures", () => avatar1.Texture != avatar2.Texture);

            AddStep("dispose children", () =>
            {
                Clear();
                avatar1.Dispose();
                avatar2.Dispose();
            });

            AddUntilStep("gl textures disposed", () => texture.ReferenceCount == 0);
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
