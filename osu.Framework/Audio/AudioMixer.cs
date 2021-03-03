// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Logging;
using osu.Framework.Statistics;

namespace osu.Framework.Audio
{
    public class AudioMixer : AdjustableAudioComponent
    {
        public List<int> MixChannels = new List<int>();
        private int mixerHandle;

        public AudioMixer()
        {
            // handle device or something idk
        }

        // public override void Dispose()
        // {
        //     base.Dispose();
        //
        //     Logger.Log("[AudioMixer] DISPOSE WTF");
        // }

        public void AddChannel(int channelHandle, bool addPaused = false)
        {
            EnqueueAction(() =>
            {
                if (mixerHandle == 0)
                {
                    Logger.Log($"[AudioMixer] Attempted to add channel ({channelHandle}) when mixer not yet initialized!");
                    return;
                }

                if (MixChannels.Contains(channelHandle))
                {
                    Logger.Log($"[AudioMixer] Channel ({channelHandle}) already added!");
                    return;
                }

                // BassMix.MixerAddChannel(mixerHandle, channelHandle, BassFlags.MixerPause | BassFlags.MixerBuffer);
                BassFlags flags = addPaused ? BassFlags.MixerChanPause | BassFlags.MixerChanBuffer : BassFlags.MixerChanBuffer;

                BassMix.MixerAddChannel(mixerHandle, channelHandle, flags);
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
            // Bass.ChannelSetPosition(sfxHandle, 0);
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
            BassMix.ChannelFlags(channelHandle, BassFlags.Default, BassFlags.MixerPause);
            Bass.ChannelSetPosition(channelHandle, 0);
        }

        public void Init()
        {
            Logger.Log("[AudioMixer] Init()");
            EnqueueAction(() =>
            {
                mixerHandle = BassMix.CreateMixerStream(44100, 2, BassFlags.MixerNonStop);
                Logger.Log($"[AudioMixer] CreateMixerStream: {Bass.LastError}");
                Bass.ChannelPlay(mixerHandle);
                Logger.Log($"[AudioMixer] ChannelPlay(mixer): {Bass.LastError}");
            });
        }

        // protected override void UpdateChildren()
        // {
        //     base.UpdateChildren();
        //
        //     Logger.Log("[AudioMixer] UpdateChildren()");
        // }

        protected override void UpdateState()
        {
            // Logger.Log("[AudioMixer] UpdateState()");
            FrameStatistics.Add(StatisticsCounterType.MixChannels, MixChannels.Count);

            base.UpdateState();
        }
    }
}
