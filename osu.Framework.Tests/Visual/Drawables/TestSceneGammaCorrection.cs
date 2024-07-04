// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneGammaCorrection : FrameworkTestScene
    {
        private FillFlowContainer interpolatingLines = null!;

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Child = new FillFlowContainer
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Margin = new MarginPadding(20),
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "Interpolation in linear space (gamma-correct)",
                        Font = FrameworkFont.Regular.With(size: 24),
                        Margin = new MarginPadding { Bottom = 10f },
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "d3.interpolateRgb.gamma(2.2)(\"red\", \"blue\")"
                    },
                    new Sprite
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(750f, 50f),
                        // Taken from https://observablehq.com/@d3/working-with-color
                        Texture = textures.Get("d3-colour-interpolation"),
                    },
                    new SpriteText
                    {
                        Margin = new MarginPadding { Top = 20f },
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = $"{nameof(Interpolation)}.{nameof(Interpolation.ValueAt)}(Red, Blue) with 1px boxes",
                    },
                    interpolatingLines = new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.X,
                        Height = 50f,
                        ChildrenEnumerable = Enumerable.Range(0, 750).Select(i => new Box
                        {
                            Width = 1f,
                            RelativeSizeAxes = Axes.Y,
                            Colour = Interpolation.ValueAt(i, Color4.Red, Color4.Blue, 0, 750),
                        }),
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "Interpolation in sRGB space (gamma-incorrect)",
                        Font = FrameworkFont.Regular.With(size: 24),
                        Margin = new MarginPadding { Top = 50f, Bottom = 10f },
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "Figma design",
                    },
                    new Sprite
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(750f, 50f),
                        Texture = textures.Get("figma-colour-interpolation"),
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Margin = new MarginPadding { Top = 20f },
                        Text = $"{nameof(ColourInfo)}.{nameof(ColourInfo.GradientHorizontal)}(Red, Blue)"
                    },
                    new Box
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(750f, 50f),
                        Colour = ColourInfo.GradientHorizontal(Color4.Red, Color4.Blue),
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "Colour blending",
                        Font = FrameworkFont.Regular.With(size: 24),
                        Margin = new MarginPadding { Top = 50f, Bottom = 10f },
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "Regular container",
                    },
                    new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(750, 50),
                        Children = new[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Red
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Colour = Color4.Black,
                                Alpha = 0.5f
                            }
                        }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "Buffered container",
                        Margin = new MarginPadding { Top = 20f },
                    },
                    new BufferedContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(750, 50),
                        Children = new[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Red
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Colour = Color4.Black,
                                Alpha = 0.5f
                            }
                        }
                    },
                }
            };
        });

        [Test]
        public void TestInterpolationInLinearSpace()
        {
            AddAssert("interpolation in linear space", () =>
            {
                var middle = interpolatingLines.Children[interpolatingLines.Children.Count / 2];
                return middle.Colour.AverageColour.Linear == new Color4(0.5f, 0f, 0.5f, 1f);
            });
        }

        public partial class SampleSpriteD3 : Sprite
        {
            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                // Taken from https://observablehq.com/@d3/working-with-color
                Texture = textures.Get("colour-interpolation");
            }
        }
    }
}
