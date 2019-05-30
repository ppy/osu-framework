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
        private FillFlowContainer textContainer;

        private FontStore fontStore;
        private TextureStore textureStore;
        private StorageBackedResourceStore storageStore;
        private TrackStore trackStore;

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
            trackStore = (TrackStore)audioManager.GetTrackStore();
            storageStore = new StorageBackedResourceStore(storage);
        }

        [Test]
        public void TestGetFontStoreResources()
        {
            var glyphNames = fontStore.GetAvailableResources().ToList();
            var glyphTextures = new List<Texture>();
            var boxes = new List<Drawable>();

            AddStep("Get all available resources", () =>
            {
                glyphTextures = new List<Texture>();
                boxes = new List<Drawable>();
                glyphTextures.AddRange(glyphNames.Select(glyph =>
                {
                    var tex = fontStore.Get(glyph);
                    boxes.Add(new Box
                    {
                        Texture = tex,
                        Size = new Vector2(20)
                    });
                    return tex;
                }));
            });
            AddStep("Print resources in font store", () => populateText(glyphNames, boxes));
            AddAssert("No glyphs were null", () => glyphTextures.All(tex => tex != null));
        }

        [Test]
        public void TestGetStorageBackedResources()
        {
            var fileNames = storageStore.GetAvailableResources().ToList();
            var fileList = new List<byte[]>();
            var texts = new List<Drawable>();

            AddStep("Get all available resources", () =>
            {
                texts = new List<Drawable>();
                fileList = new List<byte[]>();
                fileList.AddRange(fileNames.Select(file =>
                {
                    var tex = storageStore.Get(file);
                    texts.Add(new SpriteText { Text = tex.ToString() });
                    return tex;
                }));
            });
            AddStep("Print storage backed resources", () => populateText(fileNames, texts));
            AddAssert("No files were null", () => fileList.All(file => file != null));
        }

        [Test]
        public void TestGetTextureStore()
        {
            var texNames = textureStore.GetAvailableResources().ToList();
            var textures = new List<Texture>();
            var drawables = new List<Drawable>();

            AddStep("Get all available resources", () =>
            {
                textures = new List<Texture>();
                drawables = new List<Drawable>();
                textures.AddRange(texNames.Select(name =>
                {
                    var tex = textureStore.Get(name);
                    drawables.Add(new Box
                    {
                        Texture = tex,
                        Size = new Vector2(20)
                    });
                    return tex;
                }));
            });
            AddStep("Print resources in texture store", () => populateText(texNames, drawables));
            AddAssert("No textures were null", () => textures.All(tex => tex != null));
        }

        [Test]
        public void TestGetTrackManager()
        {
            var trackNames = trackStore.GetAvailableResources().ToList();
            var fileList = new List<Track>();
            var texts = new List<Drawable>();

            AddStep("Get all available resources", () =>
            {
                texts = new List<Drawable>();
                fileList = new List<Track>();
                fileList.AddRange(trackNames.Select(name =>
                {
                    var track = trackStore.Get(name);
                    texts.Add(new SpriteText { Text = track.ToString() });
                    return track;
                }));
            });
            AddStep("Print resources in track store", () => populateText(trackNames, texts));
            AddAssert("No tracks were null", () => fileList.All(file => file != null));
        }

        private void populateText(List<string> lines, List<Drawable> results)
        {
            textContainer.Clear();

            for (int i = 0; i < lines.Count; i++)
            {
                var result = results[i];
                result.Origin = Anchor.TopRight;
                result.Anchor = Anchor.TopRight;
                textContainer.Add(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new[]
                    {
                        new SpriteText
                        {
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Text = lines[i],
                        },
                        result
                    }
                });
            }
        }
    }
}
