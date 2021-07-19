// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
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

        private const int frequency = 44100;

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

        public void Init()
        {
            Logger.Log("[AudioMixer] Init()");

            MixerHandle = BassMix.CreateMixerStream(frequency, 2, BassFlags.MixerNonStop | BassFlags.Float);
            Logger.Log($"[AudioMixer] CreateMixerStream: {Bass.LastError}");
            Bass.ChannelPlay(MixerHandle);
            Logger.Log($"[AudioMixer] ChannelPlay(mixer): {Bass.LastError}");
        }

        protected override void UpdateState()
        {
            for (int i = 0; i < MixChannels.Count; i++)
            {
                var channel = MixChannels[i];

                if (Bass.ChannelIsActive(channel) == PlaybackState.Stopped)
                {
                    // HACK: avoid auto-cleanup of TrackBass channels - they are "Reverse" thanks to the tempo and reverse fx chain they have
                    if (Bass.ChannelGetInfo(channel).ChannelType == ChannelType.Reverse) return;

                    Logger.Log($"[AudioMixer] Channel gone, auto-removing ({channel})");
                    RemoveChannel(channel);
                    i--;
                }
            }

            FrameStatistics.Add(StatisticsCounterType.MixChannels, MixChannels.Count);

            base.UpdateState();
        }
    }
}
