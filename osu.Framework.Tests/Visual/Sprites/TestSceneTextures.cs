// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneTextures : FrameworkTestScene
    {
        private BlockingStoreProvidingContainer spriteContainer;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = spriteContainer = new BlockingStoreProvidingContainer { RelativeSizeAxes = Axes.Both };
        });

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("reset", () => spriteContainer.BlockingOnlineStore.Reset());
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

            AddStep("add disposable sprite", () => avatar1 = addSprite("1"));
            AddStep("add disposable sprite", () => avatar2 = addSprite("1"));

            AddUntilStep("wait for texture load", () => avatar1.Texture != null && avatar2.Texture != null);
            AddAssert("both textures are RefCount", () => avatar1.Texture is TextureWithRefCount && avatar2.Texture is TextureWithRefCount);

            AddAssert("textures share gl texture", () => avatar1.Texture.HasSameNativeTexture(avatar2.Texture));
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

            AddStep("begin blocking load", () => spriteContainer.BlockingOnlineStore.StartBlocking("1"));

            AddStep("get first", () => avatar1 = addSprite("1"));
            AddUntilStep("wait for first to begin loading", () => spriteContainer.BlockingOnlineStore.TotalInitiatedLookups == 1);

            AddStep("get second", () => avatar2 = addSprite("2"));
            AddUntilStep("wait for avatar2 load", () => avatar2.Texture != null);

            AddAssert("avatar1 not loaded", () => avatar1.Texture == null);
            AddAssert("only one lookup occurred", () => spriteContainer.BlockingOnlineStore.TotalCompletedLookups == 1);

            AddStep("unblock load", () => spriteContainer.BlockingOnlineStore.AllowLoad());

            AddUntilStep("wait for texture load", () => avatar1.Texture != null);
            AddAssert("two lookups occurred", () => spriteContainer.BlockingOnlineStore.TotalCompletedLookups == 2);
        }

        /// <summary>
        /// Tests the case where multiple lookups occur which overlap each other, for the same texture.
        /// </summary>
        [Test]
        public void TestFetchContentionSameLookup()
        {
            Avatar avatar1 = null;
            Avatar avatar2 = null;

            AddStep("begin blocking load", () => spriteContainer.BlockingOnlineStore.StartBlocking());
            AddStep("get first", () => avatar1 = addSprite("1"));
            AddStep("get second", () => avatar2 = addSprite("1"));

            AddAssert("neither are loaded", () => avatar1.Texture == null && avatar2.Texture == null);

            AddStep("unblock load", () => spriteContainer.BlockingOnlineStore.AllowLoad());
            AddUntilStep("wait for texture load", () => avatar1.Texture != null && avatar2.Texture != null);

            AddAssert("only one lookup occurred", () => spriteContainer.BlockingOnlineStore.TotalInitiatedLookups == 1);
        }

        /// <summary>
        /// Tests that a ref-counted texture gets put in a non-available state when disposed.
        /// </summary>
        [Test]
        public void TestRefCountTextureAvailability()
        {
            Texture texture = null;

            AddStep("get texture", () => texture = spriteContainer.LargeStore.Get("1"));
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

            AddStep("get texture", () => texture = spriteContainer.NormalStore.Get("1"));
            AddStep("dispose texture", () => texture.Dispose());

            AddAssert("texture is still available", () => texture.Available);
        }

        private void assertAvailability(Func<Texture> textureFunc, bool available)
            => AddAssert($"texture available = {available}", () => ((TextureWithRefCount)textureFunc()).IsDisposed == !available);

        private Avatar addSprite(string url)
        {
            var avatar = new Avatar(url);
            spriteContainer.Add(new DelayedLoadWrapper(avatar));
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

        private class BlockingResourceStore : IResourceStore<byte[]>
        {
            /// <summary>
            /// The total number of lookups requested on this store (including blocked lookups).
            /// </summary>
            public int TotalInitiatedLookups { get; private set; }

            /// <summary>
            /// The total number of completed lookups.
            /// </summary>
            public int TotalCompletedLookups { get; private set; }

            private readonly IResourceStore<byte[]> baseStore;
            private readonly ManualResetEventSlim resetEvent = new ManualResetEventSlim(true);

            private string blockingName;
            private bool blocking;

            public BlockingResourceStore(IResourceStore<byte[]> baseStore)
            {
                this.baseStore = baseStore;
            }

            /// <summary>
            /// Block load until <see cref="AllowLoad"/> is called.
            /// </summary>
            /// <param name="blockingName">If not <c>null</c> or empty, only lookups for this particular name will be blocked.</param>
            public void StartBlocking(string blockingName = null)
            {
                this.blockingName = blockingName;

                blocking = true;
                resetEvent.Reset();
            }

            public void AllowLoad()
            {
                blocking = false;
                resetEvent.Set();
            }

            public byte[] Get(string name) => getWithBlocking(name, baseStore.Get);

            public Task<byte[]> GetAsync(string name, CancellationToken cancellationToken = default) =>
                getWithBlocking(name, name1 => baseStore.GetAsync(name1, cancellationToken));

            public Stream GetStream(string name) => getWithBlocking(name, baseStore.GetStream);

            private T getWithBlocking<T>(string name, Func<string, T> getFunc)
            {
                TotalInitiatedLookups++;

                if (blocking && name == blockingName)
                {
                    if (!resetEvent.Wait(10000))
                        throw new TimeoutException("Load was not allowed in a timely fashion.");
                }

                TotalCompletedLookups++;
                return getFunc("sample-texture.png");
            }

            public void Reset()
            {
                AllowLoad();
                TotalInitiatedLookups = 0;
                TotalCompletedLookups = 0;
            }

            public IEnumerable<string> GetAvailableResources() => Enumerable.Empty<string>();

            public void Dispose()
            {
            }
        }

        private class BlockingStoreProvidingContainer : Container
        {
            [Cached]
            public TextureStore NormalStore { get; private set; }

            [Cached]
            public LargeTextureStore LargeStore { get; private set; }

            public BlockingResourceStore BlockingOnlineStore { get; private set; }

            protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            {
                var game = parent.Get<Game>();
                var host = parent.Get<GameHost>();
                var renderer = parent.Get<IRenderer>();

                BlockingOnlineStore = new BlockingResourceStore(new NamespacedResourceStore<byte[]>(game.Resources, "Textures"));
                NormalStore = new TextureStore(renderer, host.CreateTextureLoaderStore(BlockingOnlineStore));
                LargeStore = new LargeTextureStore(renderer, host.CreateTextureLoaderStore(BlockingOnlineStore));

                return base.CreateChildDependencies(parent);
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                BlockingOnlineStore?.Reset();
            }
        }
    }
}
