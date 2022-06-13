// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
    public class TestSceneColourInterpolation : FrameworkTestScene
    {
        [Test]
        public void TestColourInterpolatesInLinearSpace()
        {
            FillFlowContainer interpolatingLines = null;

            AddStep("load interpolated colours", () =>
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
                            Text = "d3.interpolateRgb.gamma(2.2)(\"red\", \"blue\")"
                        },
                        new SampleSpriteD3
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Size = new Vector2(750f, 50f),
                        },
                        new SpriteText
                        {
                            Margin = new MarginPadding { Top = 20f },
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = $"{nameof(ColourInfo)}.{nameof(ColourInfo.GradientHorizontal)}(Blue, Red)"
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
                            Margin = new MarginPadding { Top = 20f },
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = $"{nameof(Interpolation)}.{nameof(Interpolation.ValueAt)}(Red, Red) with 1px boxes",
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
                    }
                };
            });

            AddAssert("interpolation in linear space", () =>
            {
                var middle = interpolatingLines.Children[interpolatingLines.Children.Count / 2];
                return middle.Colour.AverageColour.Linear == new Color4(0.5f, 0f, 0.5f, 1f);
            });
        }

        public class SampleSpriteD3 : Sprite
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
