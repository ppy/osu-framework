// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Visualisation
{
    internal class AudioMixerOverlay : ToolWindow
    {
        private readonly Dictionary<int, ChannelStrip> channelStrips = new Dictionary<int, ChannelStrip>();
        private readonly FillFlowContainer stripContainer;

        private readonly AudioMixer mixer;

        public AudioMixerOverlay(AudioMixer mixer)
            : base("AudioMixer", "(Ctrl+F9 to toggle)")
        {
            this.mixer = mixer;

            ScrollContent.Expire();
            MainHorizontalContent.Add(new BasicScrollContainer(Direction.Horizontal)
            {
                RelativeSizeAxes = Axes.Y,
                Width = WIDTH,
                Children = new[]
                {
                    stripContainer = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                    }
                }
            });

            // Add strip for the mixer/master out
            // stripContainer.Add(new ChannelStrip(mixer.MixerHandle, mixer));
            //
            // // Add strips for existing mix channels
            // addChannels(mixer.MixChannels);
            //
            // mixer.MixChannels.CollectionChanged += updateChannels;
        }

        private void addChannels(IList items)
        {
            foreach (var item in items.Cast<int>())
            {
                if (!channelStrips.ContainsKey(item))
                {
                    channelStrips.Add(item, new ChannelStrip(item, mixer));
                    stripContainer.Add(channelStrips[item]);
                }
            }
        }

        private void removeChannels(IList items)
        {
            foreach (var item in items.Cast<int>())
            {
                if (channelStrips.ContainsKey(item))
                    stripContainer.Remove(channelStrips[item]);

                channelStrips.Remove(item);
            }
        }

        private void updateChannels(object sender, NotifyCollectionChangedEventArgs e)
        {
            Schedule(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        addChannels(e.NewItems);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        removeChannels(e.OldItems);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        stripContainer.Clear();
                        channelStrips.Clear();
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        removeChannels(e.OldItems);
                        addChannels(e.NewItems);
                        break;
                }
            });
        }

        private class ChannelStrip : CompositeDrawable
        {
            public int ChannelHandle { get; }

            private const int sample_window = 30;
            private const int peak_hold_time = 3000;

            private float maxPeak = float.MinValue;

            private double peaksLastReset;

            private readonly Box volBarL;
            private readonly Box volBarR;
            private readonly SpriteText peakText;
            private readonly SpriteText maxPeakText;
            private readonly TextFlowContainer channelInfoText;

            private readonly AudioMixer mixer;

            public ChannelStrip(int channelHandle, AudioMixer mixer)
            {
                this.mixer = mixer;

                ChannelHandle = channelHandle;
                RelativeSizeAxes = Axes.Y;
                Width = 60;
                Height = 1f;
                InternalChildren = new Drawable[]
                {
                    volBarL = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Origin = Anchor.BottomLeft,
                        Anchor = Anchor.BottomLeft,
                        Colour = Colour4.White,
                        Height = 0f,
                        Width = 0.5f,
                    },
                    volBarR = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Origin = Anchor.BottomRight,
                        Anchor = Anchor.BottomRight,
                        Colour = Colour4.White,
                        Height = 0f,
                        Width = 0.5f,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Width = 1f,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            peakText = new SpriteText { Text = "N/A", Font = FrameworkFont.Condensed.With(size: 14f) },
                            maxPeakText = new SpriteText { Text = "N/A", Font = FrameworkFont.Condensed.With(size: 14f) },
                            channelInfoText = new TextFlowContainer(s => s.Font = FrameworkFont.Condensed.With(size: 12f))
                            {
                                RelativeSizeAxes = Axes.X,
                                Width = 1f,
                                AutoSizeAxes = Axes.Y
                            },
                        }
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                if (ChannelHandle == 0) return;

                float[] levels = new float[2];

                ChannelInfo chanInfo;
                Bass.ChannelGetInfo(ChannelHandle, out chanInfo);

                if (chanInfo.ChannelType == ChannelType.Mixer)
                {
                    Bass.ChannelGetLevel(ChannelHandle, levels, 1 / 1000f * sample_window, LevelRetrievalFlags.Stereo);
                    volBarL.Colour = volBarR.Colour = Colour4.GreenYellow;

                    string chanInfoTxt = chanInfo.ChannelType.ToString();
                    channelInfoText.Text = chanInfoTxt;
                }
                else
                {
                    BassMix.ChannelGetLevel(ChannelHandle, levels, 1 / 1000f * sample_window, LevelRetrievalFlags.Stereo);
                    volBarL.Colour = volBarR.Colour = Colour4.Green;
                    channelInfoText.Text = chanInfo.ChannelType.ToString();
                }

                var curPeakL = levels[0];
                var curPeakR = levels[1];
                var curPeak = (curPeakL + curPeakR) / 2f;

                if (curPeak > maxPeak)
                    peaksLastReset = Clock.CurrentTime;

                if (Clock.CurrentTime - peaksLastReset > peak_hold_time)
                {
                    peaksLastReset = Clock.CurrentTime;
                    ResetPeaks();
                }

                maxPeak = Math.Max(maxPeak, curPeak);

                var peakDisplay = curPeak == 0 ? "-∞ " : $"{BassUtils.LevelToDb(curPeak):F}";
                var maxPeakDisplay = maxPeak == 0 ? "-∞ " : $"{BassUtils.LevelToDb(maxPeak):F}";

                volBarL.TransformTo(nameof(Drawable.Height), levelToDisplay(curPeakL), sample_window * 4);
                volBarR.TransformTo(nameof(Drawable.Height), levelToDisplay(curPeakR), sample_window * 4);
                peakText.Text = $"{peakDisplay}dB";
                maxPeakText.Text = $"{maxPeakDisplay}dB";

                peakText.Colour = BassUtils.LevelToDb(curPeak) > 0 ? Colour4.Red : Colour4.White;
                maxPeakText.Colour = BassUtils.LevelToDb(maxPeak) > 0 ? Colour4.Red : Colour4.White;
            }

            public void ResetPeaks() => maxPeak = float.MinValue;

            private float levelToDisplay(float vol) => MathF.Pow(vol, 1f / 4f);
        }
    }
}
