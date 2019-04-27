// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Testing.Drawables.Sections
{
    public class ToolbarRecordSection : ToolbarSection
    {
        private Button recordButton;
        private FillFlowContainer playbackControls;
        private TestBrowser browser;

        public ToolbarRecordSection()
        {
            AutoSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load(TestBrowser browser)
        {
            this.browser = browser;
            SpriteText maxFrameCount, currentFrame;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(10),
                Children = new Drawable[]
                {
                    playbackControls = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.X,
                        RelativeSizeAxes = Axes.Y,
                        Spacing = new Vector2(5),
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Icon = FontAwesome.Solid.Circle,
                                Colour = Color4.Red,
                                Size = new Vector2(20),
                            },
                            currentFrame = new SpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Colour = FrameworkColour.Yellow,
                                Text = "0",
                                Font = new FontUsage("Roboto", weight: "Regular", fixedWidth: true)
                            },
                            new BasicSliderBar<int>
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Height = 20,
                                Width = 250,
                                Current = browser.CurrentFrame
                            },
                            maxFrameCount = new SpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = "0",
                                Font = new FontUsage("Roboto", weight: "Regular", fixedWidth: true)
                            },
                        }
                    },
                    recordButton = new Button
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 100,
                        Action = changeState
                    },
                }
            };

            browser.RecordState.BindValueChanged(updateState, true);
            browser.CurrentFrame.ValueChanged += frame => currentFrame.Text = frame.NewValue.ToString("00000");
            browser.CurrentFrame.MaxValueChanged += maxVal => maxFrameCount.Text = maxVal.ToString("00000");
        }

        private void changeState()
        {
            if (browser.RecordState.Value == RecordState.Stopped)
                browser.RecordState.Value = RecordState.Normal;
            else
                browser.RecordState.Value = browser.RecordState.Value + 1;
        }

        private void updateState(ValueChangedEvent<RecordState> args)
        {
            switch (args.NewValue)
            {
                case RecordState.Normal:
                    recordButton.Text = "record";
                    recordButton.BackgroundColour = Color4.DarkGreen;
                    playbackControls.Hide();
                    break;
                case RecordState.Recording:
                    recordButton.Text = "stop";
                    recordButton.BackgroundColour = Color4.DarkRed;
                    playbackControls.Hide();
                    break;
                case RecordState.Stopped:
                    recordButton.Text = "reset";
                    recordButton.BackgroundColour = Color4.DarkSlateGray;
                    playbackControls.Show();
                    break;
            }
        }

        private class FrameSliderBar : BasicSliderBar<int>
        {
            private readonly SpriteText currentFrameText;
            private readonly SpriteText maxFrameText;

            public FrameSliderBar()
            {
                Add(new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Depth = float.MinValue,
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        new Label
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = "0"
                        },
                        currentFrameText = new Label
                        {
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                        },
                        maxFrameText = new Label
                        {
                            Anchor = Anchor.BottomRight,
                            Origin = Anchor.BottomRight,
                        }
                    }
                });
            }

            protected override void UpdateValue(float value)
            {
                base.UpdateValue(value);

                maxFrameText.Text = CurrentNumber.MaxValue.ToString();
                currentFrameText.Text = CurrentNumber.Value.ToString();
            }

            protected override void UpdateAfterChildren()
            {
                base.UpdateAfterChildren();
                currentFrameText.X = MathHelper.Clamp(SelectionBox.Scale.X * DrawWidth, 0, DrawWidth - currentFrameText.DrawWidth);
            }

            private class Label : SpriteText
            {
                public Label()
                {
                    Font = new FontUsage(size: 18);
                    Padding = new MarginPadding { Horizontal = 2 };
                }
            }
        }

        private class RepeatButton : Button
        {
            private ScheduledDelegate repeatDelegate;

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                repeatDelegate?.Cancel();

                if (e.Button == MouseButton.Left)
                {
                    var clickEvent = new ClickEvent(e.CurrentState, e.Button, e.ScreenSpaceMouseDownPosition) { Target = this };

                    if (!base.OnClick(clickEvent))
                        return false;

                    repeatDelegate = Scheduler.AddDelayed(() => { repeatDelegate = Scheduler.AddDelayed(() => base.OnClick(clickEvent), 100, true); }, 300);

                    return true;
                }

                return false;
            }

            protected override bool OnMouseUp(MouseUpEvent e)
            {
                repeatDelegate?.Cancel();
                return base.OnMouseUp(e);
            }

            protected override bool OnClick(ClickEvent e) => false; // Clicks aren't handled by this type of button
        }
    }
}
