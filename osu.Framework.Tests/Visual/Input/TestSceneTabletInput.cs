// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.Handlers.Tablet;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public partial class TestSceneTabletInput : FrameworkTestScene
    {
        private readonly SpriteText tabletInfo;
        private readonly TabletAreaVisualiser areaVisualizer;
        private readonly FillFlowContainer penButtonFlow;
        private readonly FillFlowContainer auxButtonFlow;
        private IBindable<TabletInfo?> tablet = new Bindable<TabletInfo?>();
        private IBindable<bool> tabletEnabled = new Bindable<bool>();
        private readonly PenThresholdTester thresholdTester;

        [Resolved]
        private FrameworkConfigManager frameworkConfigManager { get; set; } = null!;

        public TestSceneTabletInput()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    tabletInfo = new SpriteText(),
                    areaVisualizer = new TabletAreaVisualiser(),
                    penButtonFlow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    },
                    auxButtonFlow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y
                    },
                    thresholdTester = new PenThresholdTester(),
                }
            };

            for (int i = 0; i < 8; i++)
                penButtonFlow.Add(new PenButtonHandler(i));

            for (int i = 0; i < 16; i++)
                auxButtonFlow.Add(new AuxiliaryButtonHandler(i));
        }

        [Resolved]
        private GameHost host { get; set; } = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var tabletHandler = host.AvailableInputHandlers.OfType<OpenTabletDriverHandler>().FirstOrDefault();

            if (tabletHandler != null)
            {
                areaVisualizer.AreaSize.BindTo(tabletHandler.AreaSize);
                areaVisualizer.AreaOffset.BindTo(tabletHandler.AreaOffset);

                tablet = tabletHandler.Tablet.GetBoundCopy();
                tablet.BindValueChanged(_ => updateState(), true);

                tabletEnabled = tabletHandler.Enabled.GetBoundCopy();
                tabletEnabled.BindValueChanged(_ => updateState(), true);

                AddToggleStep("toggle tablet handling", t => tabletHandler.Enabled.Value = t);

                AddSliderStep("change width", 0, 1, 1f,
                    width => tabletHandler.AreaSize.Value = new Vector2(
                        tabletHandler.AreaSize.Default.X * width,
                        tabletHandler.AreaSize.Value.Y));

                AddSliderStep("change height", 0, 1, 1f,
                    height => tabletHandler.AreaSize.Value = new Vector2(
                        tabletHandler.AreaSize.Value.X,
                        tabletHandler.AreaSize.Default.Y * height));

                AddSliderStep("change X offset", 0, 1, 0.5f,
                    xOffset => tabletHandler.AreaOffset.Value = new Vector2(
                        tabletHandler.AreaSize.Default.X * xOffset,
                        tabletHandler.AreaOffset.Value.Y));

                AddSliderStep("change Y offset", 0, 1, 0.5f,
                    yOffset => tabletHandler.AreaOffset.Value = new Vector2(
                        tabletHandler.AreaOffset.Value.X,
                        tabletHandler.AreaSize.Default.Y * yOffset));

                AddSliderStep("change pen pressure threshold for click", 0, 1, 0f,
                    threshold => tabletHandler.PressureThreshold.Value = threshold);
            }

            AddToggleStep("toggle confine mode", enabled => frameworkConfigManager.SetValue(FrameworkSetting.ConfineMouseMode,
                enabled ? ConfineMouseMode.Always : ConfineMouseMode.Never));
        }

        private void updateState()
        {
            if (tabletEnabled.Value)
                tabletInfo.Text = tablet.Value != null ? $"Name: {tablet.Value.Name} Size: {tablet.Value.Size}" : "No tablet detected!";
            else
                tabletInfo.Text = "Tablet input is disabled.";

            areaVisualizer.Alpha = penButtonFlow.Alpha = auxButtonFlow.Alpha = thresholdTester.Alpha = tablet.Value != null && tabletEnabled.Value ? 1 : 0;
        }

        private partial class TabletAreaVisualiser : CompositeDrawable
        {
            public readonly Bindable<Vector2> AreaSize = new Bindable<Vector2>();
            public readonly Bindable<Vector2> AreaOffset = new Bindable<Vector2>();

            private Box fullArea = null!;
            private Container activeArea = null!;

            private SpriteText areaText = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                Margin = new MarginPadding(10);
                AutoSizeAxes = Axes.Both;
                InternalChild = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        fullArea = new Box
                        {
                            Width = AreaSize.Default.X,
                            Height = AreaSize.Default.Y,
                            Colour = FrameworkColour.GreenDark
                        },
                        activeArea = new Container
                        {
                            Origin = Anchor.Centre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = FrameworkColour.YellowGreen
                                },
                                areaText = new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                }
                            }
                        },
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                AreaSize.BindValueChanged(size =>
                {
                    activeArea.Size = size.NewValue;
                    areaText.Text = $"Active area: {size.NewValue}";
                }, true);
                AreaSize.DefaultChanged += fullSize => fullArea.Size = fullSize.NewValue;
                fullArea.Size = AreaSize.Default;

                AreaOffset.BindValueChanged(offset => activeArea.Position = offset.NewValue, true);
            }
        }

        private partial class PenButtonHandler : CompositeDrawable
        {
            private readonly TabletPenButton button;
            private readonly Drawable background;

            public PenButtonHandler(int buttonIndex)
            {
                button = TabletPenButton.Primary + buttonIndex;

                Size = new Vector2(50);

                InternalChildren = new[]
                {
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkGreen,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = $"B{buttonIndex + 1}"
                    }
                };
            }

            protected override bool OnTabletPenButtonPress(TabletPenButtonPressEvent e)
            {
                if (e.Button != button)
                    return base.OnTabletPenButtonPress(e);

                background.FadeIn(100, Easing.OutQuint);
                return true;
            }

            protected override void OnTabletPenButtonRelease(TabletPenButtonReleaseEvent e)
            {
                if (e.Button != button)
                {
                    base.OnTabletPenButtonRelease(e);
                    return;
                }

                background.FadeOut(100);
            }
        }

        private partial class AuxiliaryButtonHandler : CompositeDrawable
        {
            private readonly TabletAuxiliaryButton button;
            private readonly Drawable background;

            public AuxiliaryButtonHandler(int buttonIndex)
            {
                button = TabletAuxiliaryButton.Button1 + buttonIndex;

                Size = new Vector2(50);

                InternalChildren = new[]
                {
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkGreen,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = $"B{buttonIndex + 1}"
                    }
                };
            }

            protected override bool OnTabletAuxiliaryButtonPress(TabletAuxiliaryButtonPressEvent e)
            {
                if (e.Button != button)
                    return base.OnTabletAuxiliaryButtonPress(e);

                background.FadeIn(100, Easing.OutQuint);
                return true;
            }

            protected override void OnTabletAuxiliaryButtonRelease(TabletAuxiliaryButtonReleaseEvent e)
            {
                if (e.Button != button)
                {
                    base.OnTabletAuxiliaryButtonRelease(e);
                    return;
                }

                background.FadeOut(100);
            }
        }

        private partial class PenThresholdTester : CompositeDrawable
        {
            private Box background = null!;
            private SpriteText text = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                Size = new Vector2(100, 50);
                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    text = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                };
                setPressed(false);
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                setPressed(true);
                return true;
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                base.OnMouseUp(e);
                setPressed(false);
            }

            private void setPressed(bool pressed)
            {
                background.Colour = pressed ? FrameworkColour.Green : FrameworkColour.GreenDark;
                text.Text = pressed ? "I am pressed" : "press me";
            }
        }
    }
}
