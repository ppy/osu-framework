// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Tests.Visual.Sprites
{
    [Ignore("This test cannot be run in headless mode (a renderer is required).")]
    public partial class TestSceneTextureMipmaps : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private Game game { get; set; } = null!;

        private readonly List<string?> availableFontResources = new List<string?>();

        private FontStore fonts = null!;

        private double timeSpent;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Scheduler.CancelDelayedTasks();

            fonts?.Dispose();
            fonts = new FontStore(host.Renderer, new GlyphStore(game.Resources, @"Fonts/FontAwesome5/FontAwesome-Solid", host.CreateTextureLoaderStore(game.Resources)), useAtlas: true);

            availableFontResources.Clear();
            availableFontResources.AddRange(fonts.GetAvailableResources());

            // fetch any glyph for a texture atlas to be generated.
            getNextGlyph();

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
        public void TestAddOnce()
        {
            AddStep("add one", () =>
            {
                timeSpent = 0;

                getNextGlyph();
                uploadAndReport();
            });
        }

        [Test]
        public void TestAddGradually()
        {
            int fetchedGlyphs = 0;
            int? count = null;
            ScheduledDelegate? glyphDelegate = null;

            AddStep("add gradually", () =>
            {
                count = availableFontResources.Count;
                fetchedGlyphs = 0;

                timeSpent = 0;

                glyphDelegate?.Cancel();
                glyphDelegate = Scheduler.AddDelayed(() =>
                {
                    if (fetchedGlyphs == count)
                    {
                        if (!pendingMipmapGeneration)
                            stop();

                        return;
                    }

                    getNextGlyph();
                    upload();
                    fetchedGlyphs++;
                }, 1, true);
            });

            AddStep("stop", stop);

            void stop()
            {
                // ReSharper disable once AccessToModifiedClosure
                glyphDelegate?.Cancel();
                report($"Generating mipmaps spent {timeSpent / (fetchedGlyphs):N3}ms on average");
            }
        }

        [Test]
        public void TestAddHalf()
        {
            AddStep("add half", () =>
            {
                timeSpent = 0;

                int count = availableFontResources.Count / 2;
                for (int i = 0; i < count; i++)
                    getNextGlyph();

                uploadAndReport();
            });
        }

        [Test]
        public void TestAddAll()
        {
            AddStep("add all", () =>
            {
                timeSpent = 0;

                int count = availableFontResources.Count;
                for (int i = 0; i < count; i++)
                    getNextGlyph();

                uploadAndReport();
            });
        }

        private readonly StopwatchClock stopwatchClock = new StopwatchClock(true);

        private bool pendingMipmapGeneration;

        private void upload()
        {
            if (pendingMipmapGeneration)
                return;

            host.Renderer.ScheduleExpensiveOperation(new ScheduledDelegate(() =>
            {
                // ensure atlas texture is uploaded first before profiling mipmap generation.
                fonts.Atlas.AtlasTexture!.NativeTexture.Upload();

                switch (host.Renderer)
                {
                    case GLRenderer gl:
                        uploadOpenGL(gl);
                        break;

                    case VeldridRenderer veldrid:
                        uploadVeldrid(veldrid);
                        break;

                    default:
                        throw new InvalidOperationException("Unsupported renderer.");
                }

                pendingMipmapGeneration = false;
            }));

            pendingMipmapGeneration = true;
        }

        private void uploadOpenGL(GLRenderer gl)
        {
            double timeBefore = stopwatchClock.CurrentTime;

            fonts.Atlas.AtlasTexture!.NativeTexture.GenerateMipmaps();
            GL.Finish();

            timeSpent += stopwatchClock.CurrentTime - timeBefore;
        }

        private void uploadVeldrid(VeldridRenderer veldrid)
        {
            double timeBefore = stopwatchClock.CurrentTime;

            veldrid.MipmapGenerationCommands.Begin();
            veldrid.BypassMipmapGenerationQueue = true;

            fonts.Atlas.AtlasTexture!.NativeTexture.GenerateMipmaps();

            veldrid.BypassMipmapGenerationQueue = false;

            veldrid.BufferUpdateCommands.End();
            veldrid.Device.SubmitCommands(veldrid.BufferUpdateCommands);

            veldrid.MipmapGenerationCommands.End();
            veldrid.Device.SubmitCommands(veldrid.MipmapGenerationCommands);
            veldrid.Device.WaitForIdle();

            timeSpent += stopwatchClock.CurrentTime - timeBefore;

            veldrid.BufferUpdateCommands.Begin();
        }

        private void uploadAndReport()
        {
            upload();

            ScheduledDelegate reportDelegate = null!;

            reportDelegate = Scheduler.AddDelayed(() =>
            {
                for (int i = 10; i >= 0; --i)
                {
                    if (pendingMipmapGeneration)
                        continue;

                    report($"Generating mipmaps spent {timeSpent:N3}ms");
                    // ReSharper disable once AccessToModifiedClosure
                    reportDelegate?.Cancel();
                    break;
                }
            }, 0, true);
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

        private void getNextGlyph()
        {
            if (availableFontResources.Count == 0)
                return;

            fonts.Get(availableFontResources[0]);
            availableFontResources.RemoveAt(0);
        }

        private partial class MipmapSprite : Sprite
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
