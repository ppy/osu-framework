// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneResourceStores : FrameworkTestScene
    {
        private FontStore fontStore;
        private FillFlowContainer textContainer;
        private TextureStore textureStore;
        private StorageBackedResourceStore storageStore;
        private TrackManager trackManager;

        [BackgroundDependencyLoader]
        private void load(FontStore fontStore, Storage storage, TextureStore textureStore, AudioManager audioManager)
        {
            Child = new ScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = textContainer = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                },
            };

            this.fontStore = fontStore;
            this.textureStore = textureStore;
            trackManager = audioManager.GetTrackManager();
            storageStore = new StorageBackedResourceStore(storage);
        }

        [Test]
        public void TestGetFontStoreResources()
        {
            AddStep("Print resources in font store", () => populateText(fontStore.GetAvailableResources()));
        }

        [Test]
        public void TestGetStorageBackedResources()
        {
            AddStep("Print storage backed resources", () => populateText(storageStore.GetAvailableResources()));
        }

        [Test]
        public void TestGetTextureStore()
        {
            AddStep("Print texture store", () => populateText(textureStore.GetAvailableResources()));
        }

        [Test]
        public void TestGetTrackManager()
        {
            AddStep("Print track manager", () => populateText(trackManager.GetAvailableResources()));
        }

        [Test]
        public void TestGetFromResources()
        {
            Texture glyph = null;
            byte[] file = null;
            Texture texture = null;
            Track track = null;

            AddStep("Get all resources", () =>
            {
                glyph = null;
                file = null;
                texture = null;
                track = null;

                var glyphs = fontStore.GetAvailableResources();
                var storage = storageStore.GetAvailableResources();
                var textures = textureStore.GetAvailableResources();
                var tracks = trackManager.GetAvailableResources();

                glyph = fontStore.Get(glyphs.First());
                file = storageStore.Get(storage.First());
                texture = textureStore.Get(textures.First());
                track = trackManager.Get(tracks.First());

                textContainer.Clear();
                textContainer.Add(new Box
                {
                    Texture = glyph,
                    Size = new Vector2(20)
                });

                textContainer.Add(new SpriteText
                {
                    Text = file.ToString()
                });

                textContainer.Add(new Box
                {
                    Texture = texture,
                    Size = new Vector2(20)
                });

                textContainer.Add(new SpriteText
                {
                    Text = track.ToString()
                });
            });

            AddAssert("glyph was not null", () => glyph != null);
            AddAssert("file was not null", () => file != null);
            AddAssert("texture was not null", () => texture != null);
            AddAssert("track was not null", () => track != null);
        }

        private void populateText(IEnumerable<string> lines)
        {
            textContainer.Clear();
            foreach (var text in lines)
            {
                textContainer.Add(new SpriteText
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Text = text,
                });
            }
        }
    }
}
