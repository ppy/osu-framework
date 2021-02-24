// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using ManagedBass;
using ManagedBass.Mix;
using ManagedBass.Fx;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Tests.Audio;

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
        private readonly Box[] mixChannels = new Box[num_mix_channels];

        [BackgroundDependencyLoader]
        private void load(ITrackStore tracks)
        {
            // Create Mixer
            mixerHandle = BassMix.CreateMixerStream(44100, 2, BassFlags.MixerNonStop);
            Logger.Log($"[BASSDLL] CreateMixerStream: {Bass.LastError}");

            // Load BGM Track
            DllResourceStore resources = new DllResourceStore(typeof(TrackBassTest).Assembly);
            var bgmData = resources.Get("Resources.Tracks.sample-track.mp3");
            trackHandle = Bass.CreateStream(bgmData, 0, bgmData.Length, BassFlags.Decode | BassFlags.Loop);

            // Add BGM Track to Mixer
            BassMix.MixerAddChannel(mixerHandle, trackHandle, BassFlags.MixerPause | BassFlags.MixerBuffer);
            Logger.Log($"[BASSDLL] MixerAddChannel: {Bass.LastError}");

            // Load SFX1
            var sfxData = resources.Get("Resources.Samples.long.mp3");
            sfxHandle = Bass.CreateStream(sfxData, 0, sfxData.Length, BassFlags.Decode);

            // Add SFX1 to Mixer
            BassMix.MixerAddChannel(mixerHandle, sfxHandle, BassFlags.MixerPause | BassFlags.MixerBuffer);
            Logger.Log($"[BASSDLL] MixerAddChannel: {Bass.LastError}");

            // Load SFX2
            var sfx2Data = resources.Get("Resources.Samples.loud.wav");
            sfx2Handle = Bass.CreateStream(sfx2Data, 0, sfx2Data.Length, BassFlags.Decode);

            // Add SFX1 to Mixer
            BassMix.MixerAddChannel(mixerHandle, sfx2Handle, BassFlags.MixerPause | BassFlags.MixerBuffer);
            Logger.Log($"[BASSDLL] MixerAddChannel: {Bass.LastError}");

            // Make Mixer Go
            Bass.ChannelPlay(mixerHandle);

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ChildrenEnumerable =
                        Enumerable.Range(0, num_mix_channels)
                                  .Select(i => mixChannels[i] = new Box
                                  {
                                      RelativeSizeAxes = Axes.Both,
                                      Anchor = Anchor.BottomLeft,
                                      Origin = Anchor.BottomLeft,
                                      Height = 0,
                                      Width = 1 / (float)num_mix_channels
                                  })
                }
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
                BassMix.ChannelFlags(trackHandle, BassFlags.Default, BassFlags.MixerPause);
            });
            AddStep("stop track", () =>
            {
                BassMix.ChannelFlags(trackHandle, BassFlags.MixerPause, BassFlags.MixerPause);
            });

            AddStep("Reverb on", () =>
            {
                // reverbHandle = Bass.ChannelSetFX(mixerHandle, EffectType.Freeverb, 100);
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
                BassMix.ChannelFlags(sfxHandle, BassFlags.Default, BassFlags.MixerPause);
            });

            AddStep("Play SFX2", () =>
            {
                Bass.ChannelSetPosition(sfx2Handle, 0);
                BassMix.ChannelFlags(sfx2Handle, BassFlags.Default, BassFlags.MixerPause);
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

            var buffSizeMs = 20;

            float[] levels1 = new float[1];
            BassMix.ChannelGetLevel(trackHandle, levels1, 1 / (float)buffSizeMs, LevelRetrievalFlags.Mono);
            mixChannels[0].TransformTo(nameof(Drawable.Height), levels1[0], buffSizeMs * 4);

            float[] levels2 = new float[1];
            BassMix.ChannelGetLevel(sfxHandle, levels2, 1 / (float)buffSizeMs, LevelRetrievalFlags.Mono);
            mixChannels[1].TransformTo(nameof(Drawable.Height), levels2[0], buffSizeMs * 4);

            float[] levels3 = new float[1];
            BassMix.ChannelGetLevel(sfx2Handle, levels3, 1 / (float)buffSizeMs, LevelRetrievalFlags.Mono);
            mixChannels[2].TransformTo(nameof(Drawable.Height), levels3[0], buffSizeMs * 4);

            float[] levels4 = new float[1];
            Bass.ChannelGetLevel(mixerHandle, levels4, 1 / (float)buffSizeMs, LevelRetrievalFlags.Mono);
            mixChannels[7].TransformTo(nameof(Drawable.Height), levels4[0], buffSizeMs * 4);

            // Logger.Log($"LEVEL: {levels2[0]}");
            // Logger.Log($"[BASSDLL] ChannelGetLevel: {Bass.LastError}");
        }
    }
}
