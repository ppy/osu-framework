// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneResourceStores : FrameworkTestScene
    {
        private FillFlowContainer<ResourceDisplay> flow;

        private FontStore fontStore;
        private TextureStore textureStore;
        private Storage storage;
        private AudioManager audioManager;

        [BackgroundDependencyLoader]
        private void load(FontStore fontStore, Storage storage, TextureStore textureStore, AudioManager audioManager)
        {
            Child = new BasicScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = flow = new FillFlowContainer<ResourceDisplay>
                {
                    Margin = new MarginPadding(10),
                    Spacing = new Vector2(3),
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                },
            };

            this.fontStore = fontStore;
            this.textureStore = textureStore;
            this.audioManager = audioManager;
            this.storage = storage;
        }

        [Test]
        public void TestFontStore() => showResources(fontStore.GetAvailableResources(), fontStore.Get);

        [Test]
        public void TestStorageBackedResourceStore() => showResources(new StorageBackedResourceStore(storage));

        [Test]
        public void TestGetTextureStore() => showResources(textureStore.GetAvailableResources(), textureStore.Get);

        [Test]
        public void TestGetTrackManager() => showResources(audioManager.Tracks);

        private void showResources<T>(IResourceStore<T> store) => showResources(store.GetAvailableResources(), store.Get);

        private void showResources<T>(IEnumerable<string> resources, Func<string, T> fetch)
        {
            AddStep("list resources", () =>
            {
                flow.Clear();

                foreach (var resourceName in resources)
                    flow.Add(new ResourceDisplay(resourceName, fetch(resourceName)));
            });

            AddAssert("ensure all loaded", () => flow.Children.All(rd => rd.Resource != null));
        }

        private class ResourceDisplay : Container
        {
            public readonly object Resource;

            public ResourceDisplay(string name, object resource)
            {
                Resource = resource;

                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;

                Child = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(10),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            AutoSizeAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                            Children = new[]
                            {
                                new Box
                                {
                                    Colour = Color4.Navy,
                                    RelativeSizeAxes = Axes.Both,
                                },
                                createDisplay(resource),
                            }
                        },
                        new SpriteText { Text = name },
                    }
                };
            }

            private Drawable createDisplay(object resource)
            {
                switch (resource)
                {
                    case Texture tex:
                        return new Sprite
                        {
                            Size = new Vector2(20),
                            Texture = tex
                        };

                    default:
                        return new SpriteText { Text = resource.ToString() };
                }
            }
        }
    }
}
