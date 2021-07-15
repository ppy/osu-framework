// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using ManagedBass;
using ManagedBass.Mix;
using ManagedBass.Fx;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Tests.Audio;
using osuTK;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneBassMix : FrameworkTestScene
    {
        private int mixerHandle;
        private int trackHandle;
        private int sfxHandle;
        private int sfx2Handle;

        private int reverbHandle;
        private int compressorHandle;

        private const int num_mix_channels = 8;

        private readonly int[] channelHandles = new int[num_mix_channels];
        private readonly ChannelStrip[] channelStrips = new ChannelStrip[num_mix_channels];

        [BackgroundDependencyLoader]
        private void load(ITrackStore tracks)
        {
            DllResourceStore resources = new DllResourceStore(typeof(TrackBassTest).Assembly);

            for (int i = 0; i < num_mix_channels; i++)
            {
                channelStrips[i] = new ChannelStrip
                {
                    IsMixChannel = (i < num_mix_channels - 1),
                    Width = 1f / num_mix_channels
                };
            }

            // Create Mixer
            mixerHandle = BassMix.CreateMixerStream(44100, 2, BassFlags.MixerNonStop);
            Logger.Log($"[BASSDLL] CreateMixerStream: {Bass.LastError}");
            // Make Mixer Go
            Bass.ChannelPlay(mixerHandle);
            Logger.Log($"[BASSDLL] ChannelPlay: {Bass.LastError}");
            channelHandles[num_mix_channels - 1] = mixerHandle;

            // Load BGM Track
            var bgmData = resources.Get("Resources.Tracks.sample-track.mp3");
            trackHandle = Bass.CreateStream(bgmData, 0, bgmData.Length, BassFlags.Decode | BassFlags.Loop);

            // Add BGM Track to Mixer
            BassMix.MixerAddChannel(mixerHandle, trackHandle, BassFlags.MixerChanPause | BassFlags.MixerChanBuffer);
            Logger.Log($"[BASSDLL] MixerAddChannel: {Bass.LastError}");
            channelHandles[0] = trackHandle;

            // Load SFX1
            var sfxData = resources.Get("Resources.Samples.long.mp3");
            sfxHandle = Bass.CreateStream(sfxData, 0, sfxData.Length, BassFlags.Decode);

            // Add SFX1 to Mixer
            BassMix.MixerAddChannel(mixerHandle, sfxHandle, BassFlags.MixerChanPause | BassFlags.MixerChanBuffer);
            Logger.Log($"[BASSDLL] MixerAddChannel: {Bass.LastError}");
            channelHandles[1] = sfxHandle;

            // Load SFX2
            var sfx2Data = resources.Get("Resources.Samples.tone.wav");
            sfx2Handle = Bass.CreateStream(sfx2Data, 0, sfx2Data.Length, BassFlags.Decode);

            // Add SFX1 to Mixer
            BassMix.MixerAddChannel(mixerHandle, sfx2Handle, BassFlags.MixerChanPause | BassFlags.MixerChanBuffer);
            Logger.Log($"[BASSDLL] MixerAddChannel: {Bass.LastError}");
            channelHandles[2] = sfx2Handle;

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(1.0f),
                Children = channelStrips
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("idle", () =>
            {
                // do nothing
            });
            AddStep("start track", () =>
            {
                Bass.ChannelSetPosition(trackHandle, 0);
                BassMix.ChannelFlags(trackHandle, BassFlags.Default, BassFlags.MixerChanPause);
            });
            AddStep("stop track", () =>
            {
                BassMix.ChannelFlags(trackHandle, BassFlags.MixerChanPause, BassFlags.MixerChanPause);
            });

            AddStep("Reverb on", () =>
            {
                reverbHandle = Bass.ChannelSetFX(sfxHandle, EffectType.Freeverb, 100);
                Bass.FXSetParameters(reverbHandle, new ReverbParameters
                {
                    fDryMix = 1f,
                    fWetMix = 0.1f,
                });
                Logger.Log($"[BASSDLL] ChannelSetFX: {Bass.LastError}");
            });
            AddStep("Reverb off", () =>
            {
                Bass.ChannelRemoveFX(sfxHandle, reverbHandle);
                Logger.Log($"[BASSDLL] ChannelSetFX: {Bass.LastError}");
            });

            AddStep("Compressor on", () =>
            {
                compressorHandle = Bass.ChannelSetFX(mixerHandle, EffectType.Compressor, 1);
                Bass.FXSetParameters(compressorHandle, new CompressorParameters
                {
                    fAttack = 5,
                    fRelease = 100,
                    fThreshold = -6,
                    fGain = 0,
                    // fRatio = 4,
                });
                Logger.Log($"[BASSDLL] ChannelSetFX: {Bass.LastError}");
            });
            AddStep("Compressor off", () =>
            {
                Bass.ChannelRemoveFX(mixerHandle, compressorHandle);
                Logger.Log($"[BASSDLL] ChannelSetFX: {Bass.LastError}");
            });

            AddStep("Play SFX1", () =>
            {
                Bass.ChannelSetPosition(sfxHandle, 0);
                BassMix.ChannelFlags(sfxHandle, BassFlags.Default, BassFlags.MixerChanPause);
            });

            AddStep("Play SFX2", () =>
            {
                Bass.ChannelSetPosition(sfx2Handle, 0);
                BassMix.ChannelFlags(sfx2Handle, BassFlags.Default, BassFlags.MixerChanPause);
            });

            AddStep("Reset Peaks", () =>
            {
                foreach (var strip in channelStrips)
                {
                    strip.Reset();
                }
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            Bass.StreamFree(trackHandle);
        }

        protected override void Update()
        {
            base.Update();

            for (int i = 0; i < num_mix_channels; i++)
            {
                channelStrips[i].Handle = channelHandles[i];
            }
        }
    }

    public class ChannelStrip : CompositeDrawable
    {
        public new int Handle { get; set; }
        public int BuffSize = 30;
        public bool IsMixChannel { get; set; } = true;

        private float maxPeak = float.MinValue;
        private float peak = float.MinValue;
        private readonly Box volBarL;
        private readonly Box volBarR;
        private readonly SpriteText peakText;
        private readonly SpriteText maxPeakText;

        public ChannelStrip(int handle = -1)
        {
            Handle = handle;

            RelativeSizeAxes = Axes.Both;
            InternalChildren = new Drawable[]
            {
                volBarL = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.BottomLeft,
                    Anchor = Anchor.BottomLeft,
                    Colour = Colour4.Green,
                    Height = 0f,
                    Width = 0.5f,
                },
                volBarR = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.BottomRight,
                    Anchor = Anchor.BottomRight,
                    Colour = Colour4.Green,
                    Height = 0f,
                    Width = 0.5f,
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Children = new[]
                    {
                        peakText = new SpriteText
                        {
                            Text = $"{peak}dB",
                        },
                        maxPeakText = new SpriteText
                        {
                            Text = $"{maxPeak}dB",
                        }
                    }
                }
            };
        }

        protected override void Update()
        {
            base.Update();

            if (Handle == 0)
            {
                volBarL.Height = 0;
                peakText.Text = "N/A";
                maxPeakText.Text = "N/A";
                return;
            }

            float[] levels = new float[2];

            if (IsMixChannel)
                BassMix.ChannelGetLevel(Handle, levels, 1 / (float)BuffSize, LevelRetrievalFlags.Stereo);
            else
                Bass.ChannelGetLevel(Handle, levels, 1 / (float)BuffSize, LevelRetrievalFlags.Stereo);

            peak = (levels[0] + levels[1]) / 2f;
            maxPeak = Math.Max(peak, maxPeak);

            volBarL.TransformTo(nameof(Drawable.Height), levels[0], BuffSize * 4);
            volBarR.TransformTo(nameof(Drawable.Height), levels[1], BuffSize * 4);
            peakText.Text = $"{BassUtils.LevelToDb(peak):F}dB";
            maxPeakText.Text = $"{BassUtils.LevelToDb(maxPeak):F}dB";
        }

        public void Reset()
        {
            peak = float.MinValue;
            maxPeak = float.MinValue;
        }
    }
}
