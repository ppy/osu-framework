// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;

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
                        Text = "Frame:"
                    },
                    frameSliderBar = new BasicSliderBar<int>
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 200,
                        Colour = Color4.MediumPurple
                    },
                    previousButton = new Button
                    {
                        Width = 25,
                        RelativeSizeAxes = Axes.Y,
                        BackgroundColour = Color4.DarkMagenta,
                        Text = "<",
                        Action = previousFrame
                    },
                    nextButton = new Button
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
    }
}
