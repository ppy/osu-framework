﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneVideoLayout : GridTestScene
    {
        public TestSceneVideoLayout()
            : base(1, 4)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Cell(0, 0).Child = createTest("video - auto size", () => new TestVideo());
            Cell(0, 1).Child = createTest("video - relative size + fit", () => new TestVideo
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit
            });
            Cell(0, 2).Child = createTest("video - relative size + fill", () => new TestVideo
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fill
            });
            Cell(0, 3).Child = createTest("video - fixed size", () => new TestVideo { Size = new Vector2(100, 50) });
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
    }
}
