// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.Visualisation.Audio
{
    public class AudioChannelDisplay : CompositeDrawable
    {
        private const int sample_window = 30;
        private const int peak_hold_time = 3000;

        public readonly int ChannelHandle;

        private readonly Drawable volBarL;
        private readonly Drawable volBarR;
        private readonly SpriteText peakText;
        private readonly SpriteText maxPeakText;
        private readonly SpriteText mixerLabel;

        private float maxPeak = float.MinValue;
        private double lastMaxPeakTime;
        private readonly bool isOutputChannel;

        public AudioChannelDisplay(int channelHandle, bool isOutputChannel = false)
        {
            ChannelHandle = channelHandle;
            this.isOutputChannel = isOutputChannel;

            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;

            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize)
                },
                RowDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.AutoSize)
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5),
                            Children = new[]
                            {
                                volBarL = new Box
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    RelativeSizeAxes = Axes.Y,
                                    Width = 30,
                                    Colour = isOutputChannel ? FrameworkColour.YellowGreen : FrameworkColour.Green
                                },
                                volBarR = new Box
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    RelativeSizeAxes = Axes.Y,
                                    Width = 30,
                                    Colour = isOutputChannel ? FrameworkColour.YellowGreen : FrameworkColour.Green
                                }
                            }
                        }
                    },
                    new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                peakText = new SpriteText { Text = "N/A", Font = FrameworkFont.Condensed.With(size: 14f) },
                                maxPeakText = new SpriteText { Text = "N/A", Font = FrameworkFont.Condensed.With(size: 14f) },
                                mixerLabel = new SpriteText { Text = " ", Font = FrameworkFont.Condensed.With(size: 14f), Colour = FrameworkColour.Yellow },
                            }
                        }
                    }
                }
            };
        }

        protected override void Update()
        {
            base.Update();

            float[] levels = new float[2];

            if (isOutputChannel)
                Bass.ChannelGetLevel(ChannelHandle, levels, 1 / 1000f * sample_window, LevelRetrievalFlags.Stereo);
            else
                BassMix.ChannelGetLevel(ChannelHandle, levels, 1 / 1000f * sample_window, LevelRetrievalFlags.Stereo);

            float curPeakL = levels[0];
            float curPeakR = levels[1];
            float curPeak = (curPeakL + curPeakR) / 2f;

            if (curPeak > maxPeak || Clock.CurrentTime - lastMaxPeakTime > peak_hold_time)
            {
                lastMaxPeakTime = Clock.CurrentTime;
                maxPeak = float.MinValue;
            }

            maxPeak = Math.Max(maxPeak, curPeak);

            volBarL.ResizeHeightTo(curPeakL, sample_window * 4);
            volBarR.ResizeHeightTo(curPeakR, sample_window * 4);

            string peakDisplay = curPeak == 0 ? "-∞ " : $"{BassUtils.LevelToDb(curPeak):F}";
            string maxPeakDisplay = maxPeak == 0 ? "-∞ " : $"{BassUtils.LevelToDb(maxPeak):F}";
            peakText.Text = $"curr: {peakDisplay}dB";
            maxPeakText.Text = $"peak: {maxPeakDisplay}dB";
            peakText.Colour = BassUtils.LevelToDb(curPeak) > 0 ? Colour4.Red : Colour4.White;
            maxPeakText.Colour = BassUtils.LevelToDb(maxPeak) > 0 ? Colour4.Red : Colour4.White;
            mixerLabel.Text = isOutputChannel ? "MIXER OUT" : " ";
        }
    }
}
