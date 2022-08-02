// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneAnimationLayout : GridTestScene
    {
        public TestSceneAnimationLayout()
            : base(2, 3)
        {
            Cell(0, 0).Child = createTest("texture - auto size", () => new TestTextureAnimation());
            Cell(0, 1).Child = createTest("texture - relative size + fit", () => new TestTextureAnimation
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit
            });
            Cell(0, 2).Child = createTest("texture - fixed size", () => new TestTextureAnimation { Size = new Vector2(100, 50) });

            Cell(1, 0).Child = createTest("drawable - auto size", () => new TestDrawableAnimation());
            Cell(1, 1).Child = createTest("drawable - relative size + fit", () => new TestDrawableAnimation(Axes.Both)
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit
            });
            Cell(1, 2).Child = createTest("drawable - fixed size", () => new TestDrawableAnimation(Axes.Both) { Size = new Vector2(100, 50) });
        }

        private Drawable createTest(string name, Func<Drawable> animationCreationFunc) => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Padding = new MarginPadding(10),
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                Content = new[]
                {
                    new Drawable[]
                    {
                        new SpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = name
                        },
                    },
                    new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            BorderColour = Color4.OrangeRed,
                            BorderThickness = 2,
                            Children = new[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                    AlwaysPresent = true
                                },
                                animationCreationFunc()
                            }
                        }
                    },
                },
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) }
            }
        };

        private class TestDrawableAnimation : DrawableAnimation
        {
            public TestDrawableAnimation(Axes contentRelativeAxes = Axes.None)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                for (int i = 1; i <= 60; i++)
                {
                    var c = new Container
                    {
                        RelativeSizeAxes = contentRelativeAxes,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.SlateGray
                            },
                            new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = i.ToString()
                            }
                        }
                    };

                    if ((contentRelativeAxes & Axes.X) == 0)
                        c.Width = 100;

                    if ((contentRelativeAxes & Axes.Y) == 0)
                        c.Height = 100;

                    AddFrame(c);
                }
            }
        }

        private class TestTextureAnimation : TextureAnimation
        {
            [Resolved]
            private FontStore fontStore { get; set; }

            public TestTextureAnimation()
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                for (int i = 0; i <= 9; i++)
                    AddFrame(new Texture(fontStore.Get(null, i.ToString()[0])?.Texture) { ScaleAdjust = 1 + i / 2 }, 1000.0 / 60 * 6);
            }
        }
    }
}
