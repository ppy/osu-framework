// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneFastCircle : ManualInputManagerTestScene
    {
        private TestCircle fastCircle = null!;
        private Circle circle = null!;

        [SetUp]
        public void Setup()
        {
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 100),
                    new Dimension(),
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
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "FastCircle"
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "Circle"
                        }
                    },
                    new Drawable[]
                    {
                        fastCircle = new TestCircle
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(200),
                            Clicked = onClick
                        },
                        circle = new Circle
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(200)
                        }
                    }
                }
            };
        }

        [Test]
        public void TestInput()
        {
            testInput(new Vector2(200, 100));
            testInput(new Vector2(100, 200));
            testInput(new Vector2(200, 200));
        }

        [Test]
        public void TestSmoothness()
        {
            AddStep("Change smoothness to 0", () => fastCircle.EdgeSmoothness = circle.MaskingSmoothness = 0);
            AddStep("Change smoothness to 1", () => fastCircle.EdgeSmoothness = circle.MaskingSmoothness = 1);
            AddStep("Change smoothness to 5", () => fastCircle.EdgeSmoothness = circle.MaskingSmoothness = 5);
        }

        private void testInput(Vector2 size)
        {
            AddStep($"Resize to {size}", () =>
            {
                fastCircle.Size = circle.Size = size;
            });
            AddStep("Click outside the corner", () => clickNearCorner(-Vector2.One));
            AddAssert("input not received", () => clicked == false);
            AddStep("Click inside the corner", () => clickNearCorner(Vector2.One));
            AddAssert("input received", () => clicked);
        }

        private void clickNearCorner(Vector2 offset)
        {
            clicked = false;
            InputManager.MoveMouseTo(fastCircle.ToScreenSpace(new Vector2(fastCircle.Radius * (1f - MathF.Sqrt(0.5f))) + offset));
            InputManager.Click(MouseButton.Left);
        }

        private bool clicked;

        private void onClick() => clicked = true;

        private partial class TestCircle : FastCircle
        {
            public Action? Clicked;

            protected override bool OnClick(ClickEvent e)
            {
                base.OnClick(e);
                Clicked?.Invoke();
                return true;
            }
        }
    }
}
