// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    [System.ComponentModel.Description("sprite stretching")]
    public class TestCaseFillModes : GridTestCase
    {
        public TestCaseFillModes() : base(3, 3)
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            FillMode[] fillModes =
            {
                FillMode.Stretch,
                FillMode.Fit,
                FillMode.Fill,
            };

            float[] aspects = { 1, 2, 0.5f };

            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    Cell(i, j).AddRange(new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = $"{nameof(FillMode)}=FillMode.{fillModes[i]}, {nameof(FillAspectRatio)}={aspects[j]}",
                            TextSize = 20,
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Blue,
                                },
                                new Sprite
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Texture = texture,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    FillMode = fillModes[i],
                                    FillAspectRatio = aspects[j],
                                }
                            }
                        }
                    });
                }
            }
        }

        private Texture texture;

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            texture = store.Get(@"sample-texture");
        }

        private class PaddedBox : Container
        {
            private readonly SpriteText t1;
            private readonly SpriteText t2;
            private readonly SpriteText t3;
            private readonly SpriteText t4;

            private readonly Container content;

            protected override Container<Drawable> Content => content;

            public PaddedBox(Color4 colour)
            {
                AddRangeInternal(new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colour,
                    },
                    content = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    t1 = new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre
                    },
                    t2 = new SpriteText
                    {
                        Rotation = 90,
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.TopCentre
                    },
                    t3 = new SpriteText
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre
                    },
                    t4 = new SpriteText
                    {
                        Rotation = -90,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.TopCentre
                    }
                });

                Masking = true;
            }

            public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
            {
                t1.Text = (Padding.Top > 0 ? $"p{Padding.Top}" : string.Empty) + (Margin.Top > 0 ? $"m{Margin.Top}" : string.Empty);
                t2.Text = (Padding.Right > 0 ? $"p{Padding.Right}" : string.Empty) + (Margin.Right > 0 ? $"m{Margin.Right}" : string.Empty);
                t3.Text = (Padding.Bottom > 0 ? $"p{Padding.Bottom}" : string.Empty) + (Margin.Bottom > 0 ? $"m{Margin.Bottom}" : string.Empty);
                t4.Text = (Padding.Left > 0 ? $"p{Padding.Left}" : string.Empty) + (Margin.Left > 0 ? $"m{Margin.Left}" : string.Empty);

                return base.Invalidate(invalidation, source, shallPropagate);
            }

            protected override bool OnDrag(InputState state)
            {
                Position += state.Mouse.Delta;
                return true;
            }

            protected override bool OnDragEnd(InputState state) => true;

            protected override bool OnDragStart(InputState state) => true;
        }

        #region Test Cases

        private const float container_width = 60;
        private Box fitBox;

        /// <summary>
        /// Tests that using <see cref="FillMode.Fit"/> inside a <see cref="FlowContainer{T}"/> that is autosizing in one axis doesn't result in autosize feedback loops.
        /// Various sizes of the box are tested to ensure that non-one sizes also don't lead to erroneous sizes.
        /// </summary>
        /// <param name="value">The relative size of the box that is fitting.</param>
        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void TestFitInsideFlow(float value)
        {
            ClearInternal();
            AddInternal(new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Y,
                Width = container_width,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    fitBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit
                    },
                    // A box which forces the minimum dimension of the autosize flow container to be the horizontal dimension
                    new Box { Size = new Vector2(container_width, container_width * 2) }
                }
            });

            AddStep("Set size", () => fitBox.Size = new Vector2(value));

            var expectedSize = new Vector2(container_width * value, container_width * value);

            AddAssert("Check size before invalidate (1/2)", () => fitBox.DrawSize == expectedSize);
            AddAssert("Check size before invalidate (2/2)", () => fitBox.DrawSize == expectedSize);
            AddStep("Invalidate", () => fitBox.Invalidate());
            AddAssert("Check size after invalidate (1/2)", () => fitBox.DrawSize == expectedSize);
            AddAssert("Check size after invalidate (2/2)", () => fitBox.DrawSize == expectedSize);
        }

        #endregion
    }
}
