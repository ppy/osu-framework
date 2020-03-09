// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
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
                Spacing = new Vector2(10),
            };
        }

        protected override void PopIn()
        {
            base.PopIn();

            foreach (var tex in TextureGLAtlas.GetAllAtlases())
                addTexture(tex);

            TextureGLAtlas.TextureAdded += addTexture;
            TextureGLAtlas.TextureRemoved += removeTexture;
        }

        protected override void PopOut()
        {
            base.PopOut();

            TextureGLAtlas.TextureAdded -= addTexture;
            TextureGLAtlas.TextureRemoved -= removeTexture;
        }

        private void addTexture(TextureGLAtlas texture) => Schedule(() =>
        {
            if (panelFlow.Any(p => p.AtlasTexture == texture))
                return;

            panelFlow.Add(new TexturePanel(texture));
        });

        private void removeTexture(TextureGLAtlas texture) => Schedule(() => panelFlow.RemoveAll(p => p.AtlasTexture == texture));

        private class TexturePanel : CompositeDrawable
        {
            public readonly TextureGLAtlas AtlasTexture;

            private readonly SpriteText titleText;
            private readonly SpriteText footerText;

            public TexturePanel(TextureGLAtlas atlasTexture)
            {
                AtlasTexture = atlasTexture;
                Width = 100;
                AutoSizeAxes = Axes.Y;

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
                                new UsageBackground(atlasTexture)
                                {
                                    RelativeSizeAxes = Axes.Both,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 100,
                                    Padding = new MarginPadding(5),
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Black
                                        },
                                        new Sprite
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            FillMode = FillMode.Fit,
                                            Texture = new Texture(atlasTexture),
                                        }
                                    }
                                }
                            },
                        },
                        footerText = new SpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Font = FontUsage.Default.With(size: 16)
                        },
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                try
                {
                    titleText.Text = $"{AtlasTexture.Width}x{AtlasTexture.Height}";
                    footerText.Text = $"{AtlasTexture.TextureId}";
                }
                catch { }
            }
        }

        private class UsageBackground : Box
        {
            private readonly TextureGLAtlas atlasTexture;

            private float avgUsagesPerFrame;

            public UsageBackground(TextureGLAtlas atlasTexture)
            {
                this.atlasTexture = atlasTexture;
            }

            protected override DrawNode CreateDrawNode() => new UsageBackgroundDrawNode(this);

            private class UsageBackgroundDrawNode : BoxDrawNode
            {
                protected new UsageBackground Source => (UsageBackground)base.Source;

                private ulong lastBindCount;
                private ColourInfo drawColour;

                private TextureGLAtlas atlasTexture;

                public UsageBackgroundDrawNode(Box source)
                    : base(source)
                {
                }

                public override void ApplyState()
                {
                    base.ApplyState();

                    atlasTexture = Source.atlasTexture;
                }

                public override void Draw(Action<TexturedVertex2D> vertexAction)
                {
                    if (!atlasTexture.Available)
                        return;

                    ulong delta = atlasTexture.BindCount - lastBindCount;

                    Source.avgUsagesPerFrame = Source.avgUsagesPerFrame * 0.9f + delta * 0.1f;

                    drawColour = DrawColourInfo.Colour;
                    drawColour.ApplyChild(Interpolation.ValueAt(Source.avgUsagesPerFrame, Color4.Black.Opacity(0), Color4.Red, 0, 50));

                    base.Draw(vertexAction);

                    lastBindCount = atlasTexture.BindCount;
                }

                protected override void Blit(Action<TexturedVertex2D> vertexAction)
                {
                    DrawQuad(Texture, ScreenSpaceDrawQuad, drawColour, null, vertexAction,
                        new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height));
                }

                protected internal override bool CanDrawOpaqueInterior => false;
            }
        }
    }
}
