// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneTextures : FrameworkTestScene
    {
        [Cached]
        private TextureStore normalStore;

        [Cached]
        private LargeTextureStore largeStore;

        private BlockingOnlineStore blockingOnlineStore;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var host = parent.Get<GameHost>();

            blockingOnlineStore = new BlockingOnlineStore();

            normalStore = new TextureStore(host.CreateTextureLoaderStore(blockingOnlineStore));
            largeStore = new LargeTextureStore(host.CreateTextureLoaderStore(blockingOnlineStore));

            return base.CreateChildDependencies(parent);
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("reset online store", () => blockingOnlineStore.Reset());

            // required to drop reference counts and allow fresh lookups to occur on the LargeTextureStore.
            AddStep("dispose children", () => Clear());
        }

        /// <summary>
        /// Tests that a ref-counted texture is disposed when all references are lost.
        /// </summary>
        [Test]
        public void TestRefCountTextureDisposal()
        {
            Avatar avatar1 = null;
            Avatar avatar2 = null;
            Texture texture = null;

            AddStep("add disposable sprite", () => avatar1 = addSprite("https://a.ppy.sh/3"));
            AddStep("add disposable sprite", () => avatar2 = addSprite("https://a.ppy.sh/3"));

            AddUntilStep("wait for texture load", () => avatar1.Texture != null && avatar2.Texture != null);

            AddAssert("both textures are RefCount", () => avatar1.Texture is TextureWithRefCount && avatar2.Texture is TextureWithRefCount);

            AddAssert("textures share gl texture", () => avatar1.Texture.TextureGL == avatar2.Texture.TextureGL);
            AddAssert("textures have different refcount textures", () => avatar1.Texture != avatar2.Texture);

            AddStep("dispose children", () =>
            {
                texture = avatar1.Texture;

                Clear();
                avatar1.Dispose();
                avatar2.Dispose();
            });

            assertAvailability(() => texture, false);
        }

        /// <summary>
        /// Tests the case where multiple lookups occur for different textures, which shouldn't block each other.
        /// </summary>
        [Test]
        public void TestFetchContentionDifferentLookup()
        {
            Avatar avatar1 = null;
            Avatar avatar2 = null;

            AddStep("begin blocking load", () => blockingOnlineStore.StartBlocking("https://a.ppy.sh/3"));

            AddStep("get first", () => avatar1 = addSprite("https://a.ppy.sh/3"));
            AddUntilStep("wait for first to begin loading", () => blockingOnlineStore.TotalInitiatedLookups == 1);

            AddStep("get second", () => avatar2 = addSprite("https://a.ppy.sh/2"));

            AddUntilStep("wait for avatar2 load", () => avatar2.Texture != null);

            AddAssert("avatar1 not loaded", () => avatar1.Texture == null);
            AddAssert("only one lookup occurred", () => blockingOnlineStore.TotalCompletedLookups == 1);

            AddStep("unblock load", () => blockingOnlineStore.AllowLoad());

            AddUntilStep("wait for texture load", () => avatar1.Texture != null);
            AddAssert("two lookups occurred", () => blockingOnlineStore.TotalCompletedLookups == 2);
        }

        /// <summary>
        /// Tests the case where multiple lookups occur which overlap each other, for the same texture.
        /// </summary>
        [Test]
        public void TestFetchContentionSameLookup()
        {
            Avatar avatar1 = null;
            Avatar avatar2 = null;

            AddStep("begin blocking load", () => blockingOnlineStore.StartBlocking());
            AddStep("get first", () => avatar1 = addSprite("https://a.ppy.sh/3"));
            AddStep("get second", () => avatar2 = addSprite("https://a.ppy.sh/3"));

            AddAssert("neither are loaded", () => avatar1.Texture == null && avatar2.Texture == null);

            AddStep("unblock load", () => blockingOnlineStore.AllowLoad());

            AddUntilStep("wait for texture load", () => avatar1.Texture != null && avatar2.Texture != null);

            AddAssert("only one lookup occurred", () => blockingOnlineStore.TotalInitiatedLookups == 1);
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

            assertAvailability(() => texture, false);
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

        private void assertAvailability(Func<Texture> textureFunc, bool available)
            => AddAssert($"texture available = {available}", () => ((TextureWithRefCount)textureFunc()).IsDisposed == !available);

        private Avatar addSprite(string url)
        {
            var avatar = new Avatar(url);
            Add(new DelayedLoadWrapper(avatar));
            return avatar;
        }

        [LongRunningLoad]
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

        private class BlockingOnlineStore : OnlineStore
        {
            /// <summary>
            /// The total number of lookups requested on this store (including blocked lookups).
            /// </summary>
            public int TotalInitiatedLookups { get; private set; }

            /// <summary>
            /// The total number of completed lookups.
            /// </summary>
            public int TotalCompletedLookups { get; private set; }

            private readonly ManualResetEventSlim resetEvent = new ManualResetEventSlim(true);

            private string blockingUrl;

            /// <summary>
            /// Block load until <see cref="AllowLoad"/> is called.
            /// </summary>
            /// <param name="blockingUrl">If not <c>null</c> or empty, only lookups for this particular URL will be blocked.</param>
            public void StartBlocking(string blockingUrl = null)
            {
                this.blockingUrl = blockingUrl;
                resetEvent.Reset();
            }

            public void AllowLoad() => resetEvent.Set();

            public override byte[] Get(string url)
            {
                TotalInitiatedLookups++;

                if (string.IsNullOrEmpty(blockingUrl) || url == blockingUrl)
                    resetEvent.Wait();

                TotalCompletedLookups++;
                return base.Get(url);
            }

            public void Reset()
            {
                AllowLoad();
                TotalInitiatedLookups = 0;
                TotalCompletedLookups = 0;
            }
        }
    }
}
