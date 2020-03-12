// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Visualisation
{
    internal class TextureAtlasVisualiser : ToolWindow
    {
        private readonly FillFlowContainer<TexturePanel> panelFlow;

        public TextureAtlasVisualiser()
            : base("Texture Atlases", "(Ctrl+F3 to toggle)")
        {
            ScrollContent.Child = panelFlow = new FillFlowContainer<TexturePanel>
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(22),
                Padding = new MarginPadding(10),
            };
        }

        protected override void PopIn()
        {
            base.PopIn();

            foreach (var tex in TextureGLAtlas.GetAllAtlases())
                addTexture(tex);

            TextureGLAtlas.TextureCreated += addTexture;
        }

        protected override void PopOut()
        {
            base.PopOut();

            panelFlow.Clear();

            TextureGLAtlas.TextureCreated -= addTexture;
        }

        private void addTexture(TextureGLAtlas texture) => Schedule(() =>
        {
            if (panelFlow.Any(p => p.AtlasTexture == texture))
                return;

            panelFlow.Add(new TexturePanel(texture));
        });

        private class TexturePanel : CompositeDrawable
        {
            private readonly WeakReference<TextureGLAtlas> atlasReference;

            public TextureGLAtlas AtlasTexture => atlasReference.TryGetTarget(out var tex) ? tex : null;

            private readonly SpriteText titleText;
            private readonly SpriteText footerText;

            private readonly UsageBackground usage;

            public TexturePanel(TextureGLAtlas atlasTexture)
            {
                atlasReference = new WeakReference<TextureGLAtlas>(atlasTexture);

                Size = new Vector2(100, 132);

                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        titleText = new SpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 16)
                        },
                        new Container
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                usage = new UsageBackground(atlasReference)
                                {
                                    Size = new Vector2(100)
                                },
                            },
                        },
                        footerText = new SpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 16),
                        },
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                try
                {
                    var atlas = AtlasTexture;

                    if (atlas?.Available != true)
                    {
                        Expire();
                        return;
                    }

                    titleText.Text = $"{atlas.TextureId}. {atlas.Width}x{atlas.Height} ";
                    footerText.Text = Precision.AlmostBigger(usage.AverageUsagesPerFrame, 1) ? $"{usage.AverageUsagesPerFrame:N0} binds" : string.Empty;
                }
                catch { }
            }
        }

        private class UsageBackground : Box
        {
            private readonly WeakReference<TextureGLAtlas> atlasReference;

            private ulong lastBindCount;

            public float AverageUsagesPerFrame { get; private set; }

            public UsageBackground(WeakReference<TextureGLAtlas> atlasReference)
            {
                this.atlasReference = atlasReference;
            }

            protected override DrawNode CreateDrawNode() => new UsageBackgroundDrawNode(this);

            private class UsageBackgroundDrawNode : BoxDrawNode
            {
                protected new UsageBackground Source => (UsageBackground)base.Source;

                private ColourInfo drawColour;

                private WeakReference<TextureGLAtlas> atlasReference;

                public UsageBackgroundDrawNode(Box source)
                    : base(source)
                {
                }

                public override void ApplyState()
                {
                    base.ApplyState();

                    atlasReference = Source.atlasReference;
                }

                public override void Draw(Action<TexturedVertex2D> vertexAction)
                {
                    if (!atlasReference.TryGetTarget(out var texture))
                        return;

                    if (!texture.Available)
                        return;

                    ulong delta = texture.BindCount - Source.lastBindCount;

                    Source.AverageUsagesPerFrame = Source.AverageUsagesPerFrame * 0.9f + delta * 0.1f;

                    drawColour = DrawColourInfo.Colour;
                    drawColour.ApplyChild(
                        Precision.AlmostBigger(Source.AverageUsagesPerFrame, 1)
                            ? Interpolation.ValueAt(Source.AverageUsagesPerFrame, Color4.DarkGray, Color4.Red, 0, 200)
                            : Color4.Transparent);

                    base.Draw(vertexAction);

                    // intentionally after draw to avoid counting our own bind.
                    Source.lastBindCount = texture.BindCount;
                }

                protected override void Blit(Action<TexturedVertex2D> vertexAction)
                {
                    if (!atlasReference.TryGetTarget(out var texture))
                        return;

                    const float border_width = 4;

                    // border
                    DrawQuad(Texture, ScreenSpaceDrawQuad, drawColour, null, vertexAction);

                    var shrunkenQuad = ScreenSpaceDrawQuad.AABBFloat.Shrink(border_width);

                    // background
                    DrawQuad(Texture, shrunkenQuad, Color4.Black, null, vertexAction);

                    // atlas texture
                    texture.Bind();
                    DrawQuad(texture, shrunkenQuad, Color4.White, null, vertexAction);
                }

                protected internal override bool CanDrawOpaqueInterior => false;
            }
        }
    }
}
