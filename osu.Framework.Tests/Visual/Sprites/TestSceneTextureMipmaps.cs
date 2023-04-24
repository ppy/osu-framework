// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneTextureMipmaps : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private Game game { get; set; } = null!;

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        private FontStore fonts = null!;

        private double timeSpent;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            timeSpent = 0;

            Scheduler.CancelDelayedTasks();

            fonts?.Dispose();
            fonts = new FontStore(host.Renderer, new GlyphStore(game.Resources, @"Fonts/FontAwesome5/FontAwesome-Solid", host.CreateTextureLoaderStore(game.Resources)), useAtlas: true);

            // fetch any glyph for a texture atlas to be generated.
            fonts.Get(fonts.GetAvailableResources().First());

            Debug.Assert(fonts.Atlas.AtlasTexture != null);

            Children = new[]
            {
                new FillFlowContainer
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Margin = new MarginPadding { Left = 15f, Top = 30f },
                    Spacing = new Vector2(0f, 5f),
                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = "Mipmaps",
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new[]
                            {
                                new MipmapSprite(0)
                                {
                                    Texture = fonts.Atlas.AtlasTexture,
                                    Size = new Vector2(512, 512),
                                },
                                new MipmapSprite(1)
                                {
                                    X = 512,
                                    Texture = fonts.Atlas.AtlasTexture,
                                    Size = new Vector2(256, 256),
                                },
                                new MipmapSprite(2)
                                {
                                    X = 512,
                                    Y = 256,
                                    Texture = fonts.Atlas.AtlasTexture,
                                    Size = new Vector2(128, 128),
                                },
                                new MipmapSprite(3)
                                {
                                    X = 512,
                                    Y = 384,
                                    Texture = fonts.Atlas.AtlasTexture,
                                    Size = new Vector2(64, 64),
                                },
                            }
                        }
                    },
                }
            };
        });

        [Test]
        public void TestAddGradually()
        {
            int currentGlyphIndex = 1;
            ScheduledDelegate? glyphDelegate = null;

            AddStep("add gradually", () =>
            {
                glyphDelegate?.Cancel();
                glyphDelegate = Scheduler.AddDelayed(() =>
                {
                    if (currentGlyphIndex == 500)
                    {
                        finish();
                        return;
                    }

                    string? resource = fonts.GetAvailableResources().ElementAt(currentGlyphIndex++);
                    fonts.Get(resource);

                    upload();
                }, 1, true);
            });

            AddStep("finish", finish);

            void finish()
            {
                // ReSharper disable once AccessToModifiedClosure
                glyphDelegate?.Cancel();
                report($"Generating mipmaps spent {timeSpent / (currentGlyphIndex - 1):N3}ms on average");
            }
        }

        [Test]
        public void TestAddHalf()
        {
            AddStep("add half", () =>
            {
                foreach (string? resource in fonts.GetAvailableResources().Take(fonts.GetAvailableResources().Count() / 2))
                    fonts.Get(resource);

                upload();
                report($"Generating mipmaps of final texture atlas spent {timeSpent:N3}ms");
            });
        }

        [Test]
        public void TestAddAll()
        {
            AddStep("add all", () =>
            {
                foreach (string? resource in fonts.GetAvailableResources())
                    fonts.Get(resource);

                upload();
                report($"Generating mipmaps of final texture atlas spent {timeSpent:N3}ms");
            });
        }

        private readonly StopwatchClock stopwatchClock = new StopwatchClock(true);

        private void upload()
        {
            var tcs = new TaskCompletionSource();

            host.DrawThread.Scheduler.Add(() =>
            {
                double timeBefore = stopwatchClock.CurrentTime;
                fonts.Atlas.AtlasTexture!.NativeTexture.Upload(true);
                timeSpent += stopwatchClock.CurrentTime - timeBefore;
                tcs.SetResult();
            });

            tcs.Task.WaitSafely();
        }

        private SpriteText? reportText;

        private void report(string message)
        {
            if (reportText != null)
                Remove(reportText, true);

            Add(reportText = new SpriteText
            {
                Position = new Vector2(15f, 5f),
                Text = message
            });
        }

        private class MipmapSprite : Sprite
        {
            private readonly int level;

            public MipmapSprite(int level)
            {
                this.level = level;
            }

            protected override DrawNode CreateDrawNode() => new SingleMipmapSpriteDrawNode(this, level);

            private class SingleMipmapSpriteDrawNode : SpriteDrawNode
            {
                private readonly int level;

                public SingleMipmapSpriteDrawNode(Sprite source, int level)
                    : base(source)
                {
                    this.level = level;
                }

                protected override void Blit(IRenderer renderer)
                {
                    Texture.MipLevel = level;
                    base.Blit(renderer);
                    renderer.FlushCurrentBatch(null);
                    Texture.MipLevel = null;
                }
            }
        }
    }
}
