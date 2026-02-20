// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.Visualisation.Audio
{
    public partial class AudioChannelDisplay : CompositeDrawable
    {
        private const int sample_window = 30;
        private const int peak_hold_time = 1000;

        public readonly int ChannelHandle;

        private readonly Drawable volBarL;
        private readonly Drawable volBarR;
        private readonly Drawable peakBarL;
        private readonly Drawable peakBarR;
        private readonly SpriteText peakText;
        private readonly SpriteText maxPeakText;
        private readonly SpriteText mixerLabel;

        private float peakLevelLR = float.MinValue;
        private float peakLevelL = float.MinValue;
        private float peakLevelR = float.MinValue;
        private double lastMaxPeakTime;
        private readonly bool isOutputChannel;
        private IBindable<bool> usingGlobalMixer = null!;

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
                    [
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(3),
                            Children =
                            [
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    AutoSizeAxes = Axes.X,
                                    Height = 1,
                                    Children =
                                    [
                                        volBarL = new Box
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            RelativeSizeAxes = Axes.Y,
                                            Width = 31,
                                            Height = 0,
                                            Colour = isOutputChannel ? FrameworkColour.YellowGreen : FrameworkColour.Green,
                                        },
                                        peakBarL = new Box
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.TopLeft,
                                            RelativePositionAxes = Axes.Y,
                                            Width = 31,
                                            Height = 5f,
                                            Colour = isOutputChannel ? FrameworkColour.YellowGreen : FrameworkColour.Green,
                                        },
                                    ]
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    AutoSizeAxes = Axes.X,
                                    Height = 1,
                                    Children =
                                    [
                                        volBarR = new Box
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            RelativeSizeAxes = Axes.Y,
                                            Width = 31,
                                            Height = 0,
                                            Colour = isOutputChannel ? FrameworkColour.YellowGreen : FrameworkColour.Green,
                                        },
                                        peakBarR = new Box
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.TopLeft,
                                            RelativePositionAxes = Axes.Y,
                                            Width = 31,
                                            Height = 5f,
                                            Colour = isOutputChannel ? FrameworkColour.YellowGreen : FrameworkColour.Green,
                                        },
                                    ]
                                }
                            ]
                        }
                    ],
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

        [BackgroundDependencyLoader]
        private void load(AudioManager audioManager)
        {
            usingGlobalMixer = audioManager.UsingGlobalMixer.GetBoundCopy();
        }

        protected override void Update()
        {
            base.Update();

            float[] levels = new float[2];

            if (isOutputChannel && !usingGlobalMixer.Value)
                Bass.ChannelGetLevel(ChannelHandle, levels, 1 / 1000f * sample_window, LevelRetrievalFlags.Stereo);
            else
                BassMix.ChannelGetLevel(ChannelHandle, levels, 1 / 1000f * sample_window, LevelRetrievalFlags.Stereo);

            float curLevelL = levels[0];
            float curLevelR = levels[1];
            float curLevelLR = (curLevelL + curLevelR) / 2f;

            if (Clock.CurrentTime - lastMaxPeakTime > peak_hold_time)
            {
                lastMaxPeakTime = Clock.CurrentTime;
                peakLevelL = 0;
                peakLevelR = 0;
                peakLevelLR = 0;
            }

            peakLevelL = Math.Max(peakLevelL, curLevelL);
            peakLevelR = Math.Max(peakLevelR, curLevelR);
            peakLevelLR = Math.Max(peakLevelL, peakLevelR);

            peakBarL.MoveToY(-peakLevelL, peakBarL.Y >= -peakLevelL ? 0 : sample_window * 4);
            peakBarR.MoveToY(-peakLevelR, peakBarR.Y >= -peakLevelR ? 0 : sample_window * 4);

            volBarL.ResizeHeightTo(curLevelL, volBarL.Height <= curLevelL ? 0 : sample_window * 4);
            volBarR.ResizeHeightTo(curLevelR, volBarR.Height <= curLevelR ? 0 : sample_window * 4);

            string curDisplay = curLevelLR == 0 ? "-∞ " : $"{BassUtils.LevelToDb(curLevelLR):F}";
            string peakDisplay = peakLevelLR == 0 ? "-∞ " : $"{BassUtils.LevelToDb(peakLevelLR):F}";
            peakText.Text = $"curr: {curDisplay}dB";
            maxPeakText.Text = $"peak: {peakDisplay}dB";
            peakText.Colour = BassUtils.LevelToDb(Math.Max(curLevelL, curLevelR)) > 0 ? Colour4.Red : Colour4.White;
            maxPeakText.Colour = BassUtils.LevelToDb(peakLevelLR) > 0 ? Colour4.Red : Colour4.White;
            mixerLabel.Text = isOutputChannel ? "MIXER OUT" : " ";
        }
    }
}
