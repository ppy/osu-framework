// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.States;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace osu.Framework.Testing.Drawables.Sections
{
    public class ToolbarRecordSection : ToolbarSection
    {
        private Button previousButton;
        private Button nextButton;
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

            BasicSliderBar<int> frameSliderBar;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5),
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
                            new SpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = "Playback:"
                            },
                            frameSliderBar = new FrameSliderBar
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = 250,
                            },
                            previousButton = new RepeatButton
                            {
                                Width = 25,
                                RelativeSizeAxes = Axes.Y,
                                BackgroundColour = Color4.MediumPurple,
                                Text = "<",
                                Action = previousFrame
                            },
                            nextButton = new RepeatButton
                            {
                                Width = 25,
                                RelativeSizeAxes = Axes.Y,
                                BackgroundColour = Color4.MediumPurple,
                                Text = ">",
                                Action = nextFrame
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

            frameSliderBar.Current.BindTo(browser.CurrentFrame);
            browser.RecordState.BindValueChanged(updateState, true);
        }

        private void changeState()
        {
            if (browser.RecordState == RecordState.Stopped)
                browser.RecordState.Value = RecordState.Normal;
            else
                browser.RecordState.Value = browser.RecordState.Value + 1;
        }

        private void previousFrame() => browser.CurrentFrame.Value = browser.CurrentFrame.Value - 1;

        private void nextFrame() => browser.CurrentFrame.Value = browser.CurrentFrame.Value + 1;

        private void updateState(RecordState state)
        {
            switch (state)
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

            switch (state)
            {
                case RecordState.Normal:
                case RecordState.Recording:
                    previousButton.Enabled.Value = false;
                    nextButton.Enabled.Value = false;
                    break;
                case RecordState.Stopped:
                    previousButton.Enabled.Value = true;
                    nextButton.Enabled.Value = true;
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
                    TextSize = 18;
                    Padding = new MarginPadding { Horizontal = 2 };
                }
            }
        }

        private class RepeatButton : Button
        {
            private ScheduledDelegate repeatDelegate;

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                repeatDelegate?.Cancel();

                if (args.Button == MouseButton.Left)
                {
                    if (!base.OnClick(state))
                        return false;

                    repeatDelegate = Scheduler.AddDelayed(() => { repeatDelegate = Scheduler.AddDelayed(() => base.OnClick(state), 100, true); }, 300);

                    return true;
                }

                return false;
            }

            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
            {
                repeatDelegate?.Cancel();
                return base.OnMouseUp(state, args);
            }

            protected override bool OnClick(InputState state) => false; // Clicks aren't handled by this type of button
        }
    }
}
