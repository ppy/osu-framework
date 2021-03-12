// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Framework.Statistics;

namespace osu.Framework.Audio
{
    public class AudioMixer : AdjustableAudioComponent
    {
        public BindableList<int> MixChannels = new BindableList<int>();
        public int MixerHandle { get; protected set; }

        public bool CompressorEnabled { get; protected set; }
        public bool LimiterEnabled { get; protected set; }
        public bool FilterEnabled { get; protected set; }

        private int compressorHandle;
        private int limiterHandle;
        private int filterHandle;

        // the order in which FX are applied - higher number = earlier in chain, i.e. filter -> compressor -> limiter
        private const int limiter_priority = 1;
        private const int compressor_priority = 2;
        private const int filter_priority = 3;

        private const int frequency = 48000;

        public void AddChannel(int channelHandle, bool addPaused = false)
        {
            EnqueueAction(() =>
            {
                if (MixerHandle == 0)
                {
                    Logger.Log($"[AudioMixer] Attempted to add channel ({channelHandle}) when mixer not yet initialized!");
                    return;
                }

                if (MixChannels.Contains(channelHandle))
                {
                    Logger.Log($"[AudioMixer] Channel ({channelHandle}) already added!");
                    return;
                }

                BassFlags flags = addPaused ? BassFlags.MixerChanPause | BassFlags.MixerChanBuffer : BassFlags.MixerChanBuffer;

                BassMix.MixerAddChannel(MixerHandle, channelHandle, flags);
                var error = Bass.LastError;
                Logger.Log($"[AudioMixer] MixerAddChannel: {error}");

                if (error != Errors.OK)
                    return;

                MixChannels.Add(channelHandle);
            });
        }

        public void RemoveChannel(int channelHandle)
        {
            EnqueueAction(() =>
            {
                MixChannels.Remove(channelHandle);
                BassMix.MixerRemoveChannel(channelHandle);
                Logger.Log($"[AudioMixer] MixerRemoveChannel: {Bass.LastError}");
            });
        }

        public bool PlayChannel(int channelHandle)
        {
            BassMix.ChannelFlags(channelHandle, BassFlags.Default, BassFlags.MixerChanPause);

            return Bass.LastError == Errors.OK;
        }

        public bool PauseChannel(int channelHandle)
        {
            BassMix.ChannelFlags(channelHandle, BassFlags.MixerChanPause, BassFlags.MixerChanPause);

            return Bass.LastError == Errors.OK;
        }

        public void StopChannel(int channelHandle)
        {
            BassMix.ChannelFlags(channelHandle, BassFlags.Default, BassFlags.MixerChanPause);
            Bass.ChannelSetPosition(channelHandle, 0); // resets position and also flushes buffer
        }

        public long GetChannelPosition(int channelHandle)
        {
            return BassMix.ChannelGetPosition(channelHandle);
        }

        public bool SetChannelPosition(int channelHandle, long pos)
        {
            return BassMix.ChannelSetPosition(channelHandle, pos);
        }

        #region Compressor

        public void EnableCompressor()
        {
            if (CompressorEnabled) return;

            EnqueueAction(() =>
            {
                compressorHandle = Bass.ChannelSetFX(MixerHandle, EffectType.Compressor, compressor_priority);
                Bass.FXSetParameters(compressorHandle, new CompressorParameters
                {
                    fAttack = 5f,
                    fRelease = 100f,
                    fThreshold = -6f,
                    fGain = 0f,
                    fRatio = 4f,
                });
                Logger.Log($"[AudioMixer] EnableCompressor: {Bass.LastError}");

                CompressorEnabled = true;
            });
        }

        public void DisableCompressor()
        {
            if (!CompressorEnabled) return;

            EnqueueAction(() =>
            {
                Bass.ChannelRemoveFX(MixerHandle, compressorHandle);
                Logger.Log($"[AudioMixer] DisableCompressor: {Bass.LastError}");

                CompressorEnabled = false;
            });
        }

        public void ToggleCompressor()
        {
            if (CompressorEnabled)
                DisableCompressor();
            else
                EnableCompressor();
        }

        #endregion Compressor

        #region Limiter

        public void EnableLimiter()
        {
            if (LimiterEnabled) return;

            EnqueueAction(() =>
            {
                limiterHandle = Bass.ChannelSetFX(MixerHandle, EffectType.Compressor, limiter_priority);
                Bass.FXSetParameters(limiterHandle, new CompressorParameters
                {
                    fAttack = 0.01f,
                    fRelease = 100f,
                    fThreshold = -10f,
                    fGain = 0f,
                    fRatio = 20f,
                });
                Logger.Log($"[AudioMixer] EnableLimiter: {Bass.LastError}");

                LimiterEnabled = true;
            });
        }

        public void DisableLimiter()
        {
            if (!LimiterEnabled) return;

            EnqueueAction(() =>
            {
                Bass.ChannelRemoveFX(MixerHandle, limiterHandle);
                Logger.Log($"[AudioMixer] DisableLimiter: {Bass.LastError}");

                LimiterEnabled = false;
            });
        }

        public void ToggleLimiter()
        {
            if (LimiterEnabled)
                DisableLimiter();
            else
                EnableLimiter();
        }

        #endregion Limiter

        #region Filter

        public void EnableFilter()
        {
            if (FilterEnabled) return;

            EnqueueAction(() =>
            {
                filterHandle = Bass.ChannelSetFX(MixerHandle, EffectType.BQF, filter_priority);
                Bass.FXSetParameters(filterHandle, new BQFParameters
                {
                    lFilter = BQFType.LowPass,
                    fCenter = 150
                });
                Logger.Log($"[AudioMixer] EnableFilter: {Bass.LastError}");

                FilterEnabled = true;
            });
        }

        public void DisableFilter()
        {
            if (!FilterEnabled) return;

            EnqueueAction(() =>
            {
                Bass.ChannelRemoveFX(MixerHandle, filterHandle);
                Logger.Log($"[AudioMixer] DisableFilter: {Bass.LastError}");

                FilterEnabled = false;
            });
        }

        public void ToggleFilter()
        {
            if (FilterEnabled)
                DisableFilter();
            else
                EnableFilter();
        }

        #endregion Filter

        public void Init()
        {
            Logger.Log("[AudioMixer] Init()");
            EnqueueAction(() =>
            {
                MixerHandle = BassMix.CreateMixerStream(frequency, 2, BassFlags.MixerNonStop | BassFlags.Float);
                Logger.Log($"[AudioMixer] CreateMixerStream: {Bass.LastError}");
                Bass.ChannelPlay(MixerHandle);
                Logger.Log($"[AudioMixer] ChannelPlay(mixer): {Bass.LastError}");

                EnableCompressor();
                EnableLimiter();
            });
        }

        protected override void UpdateState()
        {
            EnqueueAction(() =>
            {
                var channels = MixChannels.ToArray();

                // not sure if we want to be doing this every UpdateState?
                foreach (var channel in channels)
                {
                    if (Bass.ChannelIsActive(channel) == PlaybackState.Stopped)
                    {
                        // HACK: avoid auto-cleanup of TrackBass channels - they are "Reverse" thanks to the tempo and reverse fx chain they have
                        var info = Bass.ChannelGetInfo(channel);
                        if (info.ChannelType == ChannelType.Reverse) return;

                        Logger.Log($"[AudioMixer] Channel gone, auto-removing ({channel})");
                        RemoveChannel(channel);
                    }
                }
            });

            FrameStatistics.Add(StatisticsCounterType.MixChannels, MixChannels.Count);

            base.UpdateState();
        }
    }
}
