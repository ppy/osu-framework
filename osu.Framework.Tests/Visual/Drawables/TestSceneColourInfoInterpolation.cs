// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneColourInfoInterpolation : FrameworkTestScene
    {
        private float left;
        private float right;
        private float top;
        private float bottom;

        private readonly Box sourceBox;
        private readonly Box resultBox;
        private readonly Container preview;

        public TestSceneColourInfoInterpolation()
        {
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[]
                {
                    new Dimension()
                },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.5f),
                    new Dimension()
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 10),
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = "Source"
                                },
                                new Container
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Size = new Vector2(200),
                                    Children = new Drawable[]
                                    {
                                        sourceBox = new Box
                                        {
                                            RelativeSizeAxes = Axes.Both
                                        },
                                        preview = new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            RelativePositionAxes = Axes.Both,
                                            Masking = true,
                                            BorderColour = Color4.Yellow,
                                            BorderThickness = 3f,
                                            Child = new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Alpha = 0,
                                                AlwaysPresent = true
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 10),
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = "Result"
                                },
                                resultBox = new Box
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Size = new Vector2(200)
                                }
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("Vertical gradient", () =>
            {
                sourceBox.Colour = vertical;
                updateState();
            });
            AddStep("Horizontal gradient", () =>
            {
                sourceBox.Colour = horizontal;
                updateState();
            });
            AddStep("Single", () =>
            {
                sourceBox.Colour = Color4.Red;
                updateState();
            });

            AddSliderStep("Left", 0f, 1f, 0f, l =>
            {
                left = l;
                updateState();
            });
            AddSliderStep("Right", 0f, 1f, 1f, r =>
            {
                right = r;
                updateState();
            });
            AddSliderStep("Top", 0f, 1f, 0f, t =>
            {
                top = t;
                updateState();
            });
            AddSliderStep("Bottom", 0f, 1f, 1f, b =>
            {
                bottom = b;
                updateState();
            });
        }

        private void updateState()
        {
            Vector2 topLeft = new Vector2(left, top);
            Vector2 size = new Vector2(right - left, bottom - top);

            preview.Position = topLeft;
            preview.Size = size;

            ColourInfo result = sourceBox.Colour.Interpolate(new Quad(left, top, size.X, size.Y));
            resultBox.Colour = result;
        }

        private static readonly ColourInfo vertical = ColourInfo.GradientVertical(Color4.Red, Color4.Blue);
        private static readonly ColourInfo horizontal = ColourInfo.GradientHorizontal(Color4.Red, Color4.Blue);
    }
}
