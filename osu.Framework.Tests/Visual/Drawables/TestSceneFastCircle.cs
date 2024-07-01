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
        private CircularContainer fastCircleMask = null!;
        private CircularContainer circleMask = null!;

        [SetUp]
        public void Setup() => Schedule(() =>
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
                        fastCircleMask = new CircularContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.TopRight,
                            Size = new Vector2(200),
                            Child = fastCircle = new TestCircle
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.Centre,
                                Size = new Vector2(200),
                                Clicked = onClick
                            }
                        },
                        circleMask = new CircularContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.TopRight,
                            Size = new Vector2(200),
                            Child = circle = new Circle
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.Centre,
                                Size = new Vector2(200)
                            }
                        },
                    }
                }
            };
        });

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

        [Test]
        public void TestNestedMasking()
        {
            AddToggleStep("Toggle parent masking", m => fastCircleMask.Masking = circleMask.Masking = m);
        }

        [Test]
        public void TestRotation()
        {
            resize(new Vector2(200, 100));
            AddToggleStep("Toggle rotation", rotate =>
            {
                fastCircle.ClearTransforms();
                circle.ClearTransforms();

                if (rotate)
                {
                    fastCircle.Spin(2000, RotationDirection.Clockwise);
                    circle.Spin(2000, RotationDirection.Clockwise);
                }
            });
        }

        [Test]
        public void TestShear()
        {
            resize(new Vector2(200, 100));
            AddToggleStep("Toggle shear", shear =>
            {
                fastCircle.Shear = circle.Shear = shear ? new Vector2(0.5f, 0) : Vector2.Zero;
            });
        }

        [Test]
        public void TestScale()
        {
            resize(new Vector2(200, 100));
            AddToggleStep("Toggle scale", scale =>
            {
                fastCircle.Scale = circle.Scale = scale ? new Vector2(2f, 1f) : Vector2.One;
            });
        }

        private void testInput(Vector2 size)
        {
            resize(size);
            AddStep("Click outside the corner", () => clickNearCorner(-Vector2.One));
            AddAssert("input not received", () => clicked == false);
            AddStep("Click inside the corner", () => clickNearCorner(Vector2.One));
            AddAssert("input received", () => clicked);
        }

        private void resize(Vector2 size)
        {
            AddStep($"Resize to {size}", () =>
            {
                fastCircle.Size = circle.Size = size;
            });
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
