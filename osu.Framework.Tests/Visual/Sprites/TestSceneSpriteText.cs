// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneSpriteText : FrameworkTestScene
    {
        public TestSceneSpriteText()
        {
            FillFlowContainer flow;

            Children = new Drawable[]
            {
                new BasicScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        flow = new FillFlowContainer
                        {
                            Anchor = Anchor.TopLeft,
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Vertical,
                        }
                    }
                }
            };

            flow.Add(new SpriteText
            {
                Text = @"the quick red fox jumps over the lazy brown dog"
            });
            flow.Add(new SpriteText
            {
                Text = @"THE QUICK RED FOX JUMPS OVER THE LAZY BROWN DOG"
            });
            flow.Add(new SpriteText
            {
                Text = @"0123456789!@#$%^&*()_-+-[]{}.,<>;'\"
            });
            flow.Add(new CssTextComparison("Hello, せかい！", "css-text"));
            flow.Add(new CssTextComparison("せかい、hello!", "css-text2"));

            for (int i = 1; i <= 200; i++)
            {
                SpriteText text = new SpriteText
                {
                    Text = $@"Font testy at size {i}",
                    Font = new FontUsage("Roboto", i, i % 4 > 1 ? "Bold" : "Regular", i % 2 == 1),
                    AllowMultiline = true,
                    RelativeSizeAxes = Axes.X,
                };

                flow.Add(text);

                SpriteText cssText = new SpriteText
                {
                    Text = $@"Font testy at size {i} (CSS)",
                    Font = new FontUsage("Roboto", i, i % 4 > 1 ? "Bold" : "Regular", i % 2 == 1, css: true),
                    AllowMultiline = true,
                    RelativeSizeAxes = Axes.X,
                };

                flow.Add(cssText);
            }
        }

        /// <summary>
        /// Displays a font-mixed <see cref="SpriteText"/> followed by a captured image of the same text drawn in CSS,
        /// with a baseline visualiser placed at the correct expected position.
        /// </summary>
        private class CssTextComparison : FillFlowContainer
        {
            private readonly string text;
            private readonly string sprite;

            private const float font_size = 50;

            public CssTextComparison(string text, string sprite)
            {
                this.text = text;
                this.sprite = sprite;
            }

            [BackgroundDependencyLoader]
            private void load(TextureStore textures, FontStore fonts)
            {
                var noto = fonts.GetFont("Noto-Basic");

                Debug.Assert(noto.Metrics != null && noto.Baseline != null);

                var notoMetrics = noto.Metrics.Value;
                float notoFontSize = font_size * notoMetrics.GlyphScale;

                // The baseline expected for the sprite text would be of Noto's font (containing japanese glyphs).
                // As it has a higher baseline compared to Roboto.
                float expectedBaseline = (noto.Baseline.Value / FontStore.BASE_FONT_SIZE) * notoFontSize;

                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Vertical;
                Margin = new MarginPadding { Vertical = 25f };

                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Anchor = Anchor.TopLeft,
                                Origin = Anchor.TopLeft,
                                Scale = new Vector2(1.5f),
                                Text = "o!f (with CSS scaling)"
                            },
                            new SpriteText
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Scale = new Vector2(1.5f),
                                Text = "CSS"
                            },
                        }
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Colour = FrameworkColour.Green,
                                        Font = FrameworkFont.Regular.With(size: font_size, css: true),
                                        Text = text,
                                    },
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Margin = new MarginPadding { Horizontal = 25f },
                                        Width = 2f,
                                    },
                                    new Sprite
                                    {
                                        Colour = FrameworkColour.Yellow,
                                        // generated via https://gist.github.com/frenzibyte/9e73ee324ac1368456695cc2a75db74b.
                                        // (with the displayed text and canvas size updated per usage accordingly)
                                        Texture = textures.Get(sprite),
                                    }
                                }
                            },
                            new Box
                            {
                                Y = expectedBaseline,
                                Colour = Color4.Red,
                                RelativeSizeAxes = Axes.X,
                                Height = 2f,
                            },
                        }
                    },
                };
            }
        }
    }
}
