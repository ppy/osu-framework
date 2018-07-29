// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Configuration;
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

namespace osu.Framework.Testing.Drawables
{
    public class ToolbarPlaybackSection : CompositeDrawable
    {
        private readonly Bindable<TestBrowser.PlaybackState> playback = new Bindable<TestBrowser.PlaybackState>();
        private readonly BindableInt currentFrame = new BindableInt();

        private Button previousButton;
        private Button nextButton;
        private Button recordButton;

        public ToolbarPlaybackSection()
        {
            AutoSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load(TestBrowser.PlaybackBindable playback, TestBrowser.FrameBindable currentFrame)
        {
            this.playback.BindTo(playback);
            this.currentFrame.BindTo(currentFrame);

            BasicSliderBar<int> frameSliderBar;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Margin = new MarginPadding { Top = -5 }, // A bit of positional alignment
                        Text = "Frame:"
                    },
                    frameSliderBar = new FrameSliderBar
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 200,
                        TintColour = Color4.MediumPurple
                    },
                    previousButton = new RepeatButton
                    {
                        Width = 25,
                        RelativeSizeAxes = Axes.Y,
                        BackgroundColour = Color4.DarkMagenta,
                        Text = "<",
                        Action = previousFrame
                    },
                    nextButton = new RepeatButton
                    {
                        Width = 25,
                        RelativeSizeAxes = Axes.Y,
                        BackgroundColour = Color4.DarkMagenta,
                        Text = ">",
                        Action = nextFrame
                    },
                    recordButton = new Button
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 150,
                        BackgroundColour = Color4.Purple,
                        Action = changeState
                    },
                }
            };

            frameSliderBar.Current.BindTo(this.currentFrame);
            this.playback.BindValueChanged(updateState, true);
        }

        private void changeState()
        {
            if (playback.Value == TestBrowser.PlaybackState.Stopped)
                playback.Value = TestBrowser.PlaybackState.Normal;
            else
                playback.Value = playback.Value + 1;
        }

        private void previousFrame() => currentFrame.Value = currentFrame.Value - 1;

        private void nextFrame() => currentFrame.Value = currentFrame.Value + 1;

        private void updateState(TestBrowser.PlaybackState state)
        {
            switch (state)
            {
                case TestBrowser.PlaybackState.Normal:
                    recordButton.Text = "start recording";
                    recordButton.BackgroundColour = Color4.DarkGreen;
                    break;
                case TestBrowser.PlaybackState.Recording:
                    recordButton.Text = "stop recording";
                    recordButton.BackgroundColour = Color4.DarkRed;
                    break;
                case TestBrowser.PlaybackState.Stopped:
                    recordButton.Text = "reset";
                    recordButton.BackgroundColour = Color4.DarkOrange;
                    break;
            }

            switch (state)
            {
                case TestBrowser.PlaybackState.Normal:
                case TestBrowser.PlaybackState.Recording:
                    previousButton.Enabled.Value = false;
                    nextButton.Enabled.Value = false;
                    break;
                case TestBrowser.PlaybackState.Stopped:
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
                Box.Anchor = Anchor.CentreLeft;
                Box.Origin = Anchor.CentreLeft;
                Box.Height = 0.25f;

                SelectionBox.Anchor = Anchor.CentreLeft;
                SelectionBox.Origin = Anchor.CentreLeft;
                SelectionBox.Height = 0.25f;

                CornerRadius = 0;
                Masking = false;

                Add(new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.25f,
                    Children = new[]
                    {
                        new SpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.TopLeft,
                            TextSize = 18,
                            Text = "0"
                        },
                        currentFrameText = new SpriteText
                        {
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.BottomCentre,
                            TextSize = 18,
                            Text = "0",
                        },
                        maxFrameText = new SpriteText
                        {
                            Anchor = Anchor.BottomRight,
                            Origin = Anchor.TopRight,
                            TextSize = 18,
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
                currentFrameText.X = MathHelper.Clamp(Box.Scale.X * DrawWidth, currentFrameText.DrawWidth / 2f, DrawWidth - currentFrameText.DrawWidth / 2f);
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

                    repeatDelegate = Scheduler.AddDelayed(() =>
                    {
                        repeatDelegate = Scheduler.AddDelayed(() => base.OnClick(state), 100, true);
                    }, 300);

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
