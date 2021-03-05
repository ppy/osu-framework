// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneBassMix2 : FrameworkTestScene
    {
        private AudioManager audio;

        private TrackBass bassTrack;
        private ITrackStore tracks;
        private DrawableSample sample;

        private MixerDrawable mixerDrawable;

        [BackgroundDependencyLoader]
        private void load(ITrackStore tracks, AudioManager audio)
        {
            this.tracks = tracks;
            this.audio = audio;

            Child = mixerDrawable = new MixerDrawable(audio.Mixer);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("idle", () =>
            {
                // do nothing
            });
            AddStep("load", () =>
            {
                bassTrack = (TrackBass)tracks.Get("sample-track.mp3");
            });
            AddStep("play", () =>
            {
                bassTrack?.Start();
            });
            AddStep("stop", () =>
            {
                bassTrack?.Stop();
            });
            AddStep("Play SFX1", () =>
            {
                sample = new DrawableSample(audio.Samples.Get("long.mp3"));
                sample?.Play();
            });
            AddStep("Reset Peaks", () =>
            {
                mixerDrawable.ResetPeaks();
            });
        }

        public class MixerDrawable : CompositeDrawable
        {
            private Dictionary<int, ChannelStripDrawable> channelStrips = new Dictionary<int, ChannelStripDrawable>();
            private readonly FillFlowContainer stripContainer;

            public MixerDrawable(AudioMixer mixer)
            {
                RelativeSizeAxes = Axes.Both;
                InternalChild = new BasicScrollContainer(Direction.Horizontal)
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        stripContainer = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                        }
                    }
                };

                stripContainer.Add(new ChannelStripDrawable(mixer.MixerHandle));

                mixer.MixChannels.CollectionChanged += updateChannels;
            }

            private void addChannels(IList items)
            {
                foreach (var item in items.Cast<int>())
                {
                    if (!channelStrips.ContainsKey(item))
                    {
                        channelStrips.Add(item, new ChannelStripDrawable(item));
                        stripContainer.Add(channelStrips[item]);
                    }
                }
            }

            private void removeChannels(IList items)
            {
                foreach (var item in items.Cast<int>())
                {
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

            public void ResetPeaks()
            {
                foreach (var entry in channelStrips)
                {
                    entry.Value.ResetPeaks();
                }
            }
        }

        public class ChannelStripDrawable : CompositeDrawable
        {
            public int Handle { get; protected set; }
            public int BuffSize = 30;

            private float maxPeak = float.MinValue;
            private float peak = float.MinValue;
            private readonly Box volBarL;
            private readonly Box volBarR;
            private readonly SpriteText peakText;
            private readonly SpriteText maxPeakText;
            private readonly TextFlowContainer channelInfoText;

            public ChannelStripDrawable(int handle = 0)
            {
                Handle = handle;

                RelativeSizeAxes = Axes.Y;
                Width = 80;
                Height = 1f;
                InternalChildren = new Drawable[]
                {
                    volBarL = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Origin = Anchor.BottomLeft,
                        Anchor = Anchor.BottomLeft,
                        Colour = Colour4.White,
                        Height = 1f,
                        Width = 0.5f,
                    },
                    volBarR = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Origin = Anchor.BottomRight,
                        Anchor = Anchor.BottomRight,
                        Colour = Colour4.White,
                        Height = 1f,
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
                            peakText = new SpriteText { Text = "N/A" },
                            maxPeakText = new SpriteText { Text = "N/A" },
                            channelInfoText = new TextFlowContainer(s => s.Font = FrameworkFont.Condensed.With(size: 14f))
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

                if (Handle == 0) return;

                float[] levels = new float[2];

                ChannelInfo chanInfo;
                Bass.ChannelGetInfo(Handle, out chanInfo);

                if (chanInfo.ChannelType == ChannelType.Mixer)
                {
                    Bass.ChannelGetLevel(Handle, levels, 1f / BuffSize, LevelRetrievalFlags.Stereo);
                    volBarL.Colour = volBarR.Colour = Colour4.GreenYellow;
                }
                else
                {
                    BassMix.ChannelGetLevel(Handle, levels, 1f / BuffSize, LevelRetrievalFlags.Stereo);
                    volBarL.Colour = volBarR.Colour = Colour4.Green;
                }

                peak = (levels[0] + levels[1]) / 2f;
                maxPeak = Math.Max(peak, maxPeak);

                volBarL.TransformTo(nameof(Drawable.Height), levels[0], BuffSize * 4);
                volBarR.TransformTo(nameof(Drawable.Height), levels[1], BuffSize * 4);
                peakText.Text = $"{BassUtils.LevelToDb(peak):F}dB";
                maxPeakText.Text = $"{BassUtils.LevelToDb(maxPeak):F}dB";
                channelInfoText.Text = chanInfo.ChannelType.ToString();
            }

            public void ResetPeaks()
            {
                peak = float.MinValue;
                maxPeak = float.MinValue;
            }
        }
    }
}
