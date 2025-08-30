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
        private readonly TabletAreaVisualiser inputAreaVisualizer;
        private readonly TabletAreaVisualiser outputAreaVisualizer;
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
                    inputAreaVisualizer = new TabletAreaVisualiser(Vector2.One, "Input area", false),
                    outputAreaVisualizer = new TabletAreaVisualiser(new Vector2(256, 144), "Output area", true),
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
                inputAreaVisualizer.AreaSize.BindTo(tabletHandler.AreaSize);
                inputAreaVisualizer.AreaOffset.BindTo(tabletHandler.AreaOffset);
                inputAreaVisualizer.AreaRotation.BindTo(tabletHandler.Rotation);
                outputAreaVisualizer.AreaSize.BindTo(tabletHandler.OutputAreaSize);
                outputAreaVisualizer.AreaOffset.BindTo(tabletHandler.OutputAreaOffset);

                tablet = tabletHandler.Tablet.GetBoundCopy();
                tablet.BindValueChanged(_ => updateState(), true);

                tabletEnabled = tabletHandler.Enabled.GetBoundCopy();
                tabletEnabled.BindValueChanged(_ => updateState(), true);

                AddToggleStep("toggle tablet handling", t => tabletHandler.Enabled.Value = t);

                AddSliderStep("change input width", 0, 1, 1f,
                    width => tabletHandler.AreaSize.Value = new Vector2(
                        tabletHandler.AreaSize.Default.X * width,
                        tabletHandler.AreaSize.Value.Y));

                AddSliderStep("change input height", 0, 1, 1f,
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

                AddSliderStep("change output width", 0, 1, 1f,
                    width => tabletHandler.OutputAreaSize.Value = new Vector2(
                        width, tabletHandler.OutputAreaSize.Value.Y));

                AddSliderStep("change output height", 0, 1, 1f,
                    height => tabletHandler.OutputAreaSize.Value = new Vector2(
                        tabletHandler.OutputAreaSize.Value.X, height));

                AddSliderStep("change output X offset", 0, 1, 0.5f,
                    x => tabletHandler.OutputAreaOffset.Value = new Vector2(
                        x, tabletHandler.OutputAreaOffset.Value.Y));

                AddSliderStep("change output Y offset", 0, 1, 0.5f,
                    y => tabletHandler.OutputAreaOffset.Value = new Vector2(
                        tabletHandler.OutputAreaOffset.Value.X, y));

                AddSliderStep("change rotation", 0, 360, 0f,
                    rotation => tabletHandler.Rotation.Value = rotation);

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

            float areaVisualizerAlpha = penButtonFlow.Alpha = auxButtonFlow.Alpha = thresholdTester.Alpha = tablet.Value != null && tabletEnabled.Value ? 1 : 0;
            inputAreaVisualizer.Alpha = areaVisualizerAlpha;
            outputAreaVisualizer.Alpha = areaVisualizerAlpha;
        }

        private partial class TabletAreaVisualiser : CompositeDrawable
        {
            public TabletAreaVisualiser(Vector2 areaScale, string label, bool confineArea)
            {
                this.areaScale = areaScale;
                this.label = label;
                this.confineArea = confineArea;
            }

            private readonly Vector2 areaScale;
            private readonly string label;
            private readonly bool confineArea;

            public readonly Bindable<Vector2> AreaSize = new Bindable<Vector2>();
            public readonly Bindable<Vector2> AreaOffset = new Bindable<Vector2>();
            public readonly Bindable<float> AreaRotation = new Bindable<float>();

            private Box fullArea = null!;
            private Container activeArea = null!;
            private Box activeAreaBox = null!;

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
                                activeAreaBox = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Colour = FrameworkColour.YellowGreen,
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

                AreaSize.DefaultChanged += _ => updateVisualiser();
                AreaSize.BindValueChanged(_ => updateVisualiser());
                AreaOffset.BindValueChanged(_ => updateVisualiser());
                AreaRotation.BindValueChanged(_ =>
                {
                    activeAreaBox.Rotation = AreaRotation.Value;
                }, true);
                updateVisualiser();
            }

            private void updateVisualiser()
            {
                activeArea.Size = AreaSize.Value * areaScale;
                areaText.Text = $"{label}: {activeArea.Size}";
                fullArea.Size = AreaSize.Default * areaScale;

                // Handles the difference in positioning behavior between Input Area and Output Area
                if (confineArea)
                {
                    Vector2 offsetFromCenter = (AreaOffset.Value - new Vector2(0.5f, 0.5f)) * (AreaSize.Default - AreaSize.Value) * areaScale;
                    activeArea.Position = (fullArea.Size / 2) + offsetFromCenter;
                }
                else
                {
                    activeArea.Position = AreaOffset.Value;
                }
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
