// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneSafeArea : FrameworkTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(SafeAreaSnappingContainer),
            typeof(SafeAreaTargetContainer),
            typeof(EdgeSnappingContainer),
            typeof(SnapTargetContainer)
        };

        private readonly Bindable<MarginPadding> safeAreaPadding = new BindableMarginPadding();

        private readonly Container container;
        private readonly SpriteText textbox;

        private IWindow window;

        public TestSceneSafeArea()
        {
            Child = new FillFlowContainer
            {
                Padding = new MarginPadding(10),
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(5f),
                Children = new Drawable[]
                {
                    textbox = new SpriteText { Text = "SafeAreaPadding:" },
                    new MarginPaddingSliderContainer(safeAreaPadding)
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Colour = Color4.Red,
                                Size = new Vector2(600, 400)
                            },
                            container = new Container
                            {
                                Size = new Vector2(600, 400),
                                Child = new Box
                                {
                                    Colour = Color4.Blue,
                                    RelativeSizeAxes = Axes.Both
                                }
                            }
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            window = host.Window;

            if (window == null) return;

            safeAreaPadding.ValueChanged += e => safeAreaPaddingChanged(e.NewValue);
        }

        private void safeAreaPaddingChanged(MarginPadding padding)
        {
            container.Padding = padding;
            textbox.Text = $"SafeAreaPadding: {padding}";
        }

        private class MarginPaddingSliderContainer : FillFlowContainer
        {
            private readonly Bindable<float> safeAreaPaddingTop;
            private readonly Bindable<float> safeAreaPaddingBottom;
            private readonly Bindable<float> safeAreaPaddingLeft;
            private readonly Bindable<float> safeAreaPaddingRight;

            private readonly Bindable<MarginPadding> bindableMarginPadding;

            public MarginPaddingSliderContainer(Bindable<MarginPadding> bindableMarginPadding)
            {
                this.bindableMarginPadding = bindableMarginPadding;

                safeAreaPaddingTop = new BindableFloat { MinValue = 0, MaxValue = 200 };
                safeAreaPaddingBottom = new BindableFloat { MinValue = 0, MaxValue = 200 };
                safeAreaPaddingLeft = new BindableFloat { MinValue = 0, MaxValue = 200 };
                safeAreaPaddingRight = new BindableFloat { MinValue = 0, MaxValue = 200 };

                Direction = FillDirection.Vertical;
                Spacing = new Vector2(10);
                Children = new Drawable[]
                {
                    new SpriteText { Text = "Top" },
                    new MarginPaddingSlider { Current = safeAreaPaddingTop },
                    new SpriteText { Text = "Bottom" },
                    new MarginPaddingSlider { Current = safeAreaPaddingBottom },
                    new SpriteText { Text = "Left" },
                    new MarginPaddingSlider { Current = safeAreaPaddingLeft },
                    new SpriteText { Text = "Right" },
                    new MarginPaddingSlider { Current = safeAreaPaddingRight },
                };

                safeAreaPaddingTop.ValueChanged += updateMarginPadding;
                safeAreaPaddingBottom.ValueChanged += updateMarginPadding;
                safeAreaPaddingLeft.ValueChanged += updateMarginPadding;
                safeAreaPaddingRight.ValueChanged += updateMarginPadding;
            }

            private void updateMarginPadding(ValueChangedEvent<float> e)
            {
                bindableMarginPadding.Value = new MarginPadding
                {
                    Top = safeAreaPaddingTop.Value,
                    Bottom = safeAreaPaddingBottom.Value,
                    Left = safeAreaPaddingLeft.Value,
                    Right = safeAreaPaddingRight.Value,
                };
            }

            private class MarginPaddingSlider : BasicSliderBar<float>
            {
                public MarginPaddingSlider()
                {
                    Size = new Vector2(100, 10);
                }
            }
        }
    }
}
