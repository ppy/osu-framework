// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Statistics;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// A BASSmix audio mixer.
    /// </summary>
    public class BassAudioMixer : AudioMixer, IBassAudio, IBassAudioMixer
    {
        private readonly List<IBassAudioChannel> mixedChannels = new List<IBassAudioChannel>();
        private int mixerHandle;

        private const int frequency = 44100;

        protected override void AddInternal(IAudioChannel channel)
        {
            if (!(channel is IBassAudioChannel bassChannel))
                throw new ArgumentException($"Can only add {nameof(IBassAudioChannel)}s to a {nameof(BassAudioMixer)}.");

            EnqueueAction(() =>
            {
                if (mixedChannels.Contains(bassChannel))
                    return;

                mixedChannels.Add(bassChannel);

                if (mixerHandle == 0 || bassChannel.Handle == 0)
                    return;

                ((IBassAudioMixer)this).RegisterChannel(bassChannel);
            });
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            if (!(channel is IBassAudioChannel bassChannel))
                throw new ArgumentException($"Can only remove {nameof(IBassAudioChannel)}s from a {nameof(BassAudioMixer)}.");

            EnqueueAction(() =>
            {
                if (!mixedChannels.Remove(bassChannel))
                    return;

                if (mixerHandle == 0 || bassChannel.Handle == 0)
                    return;

                BassMix.MixerRemoveChannel(bassChannel.Handle);
                BassUtils.CheckFaulted(true);
            });
        }

        void IBassAudioMixer.RegisterChannel(IBassAudioChannel channel)
        {
            Trace.Assert(CanPerformInline);
            Trace.Assert(channel.Handle != 0);

            if (mixerHandle == 0)
                return;

            if (!mixedChannels.Contains(channel))
                throw new InvalidOperationException("Channel needs to be added to the mixer first.");

            BassMix.MixerAddChannel(mixerHandle, channel.Handle, BassFlags.MixerChanPause | BassFlags.MixerChanBuffer);
            BassUtils.CheckFaulted(true);
        }

        bool IBassAudioMixer.PlayChannel(IBassAudioChannel channel)
        {
            BassMix.ChannelFlags(channel.Handle, BassFlags.Default, BassFlags.MixerChanPause);
            return Bass.LastError == Errors.OK;
        }

        bool IBassAudioMixer.PauseChannel(IBassAudioChannel channel)
        {
            BassMix.ChannelFlags(channel.Handle, BassFlags.MixerChanPause, BassFlags.MixerChanPause);
            return Bass.LastError == Errors.OK;
        }

        void IBassAudioMixer.StopChannel(IBassAudioChannel channel)
        {
            BassMix.ChannelFlags(channel.Handle, BassFlags.MixerChanPause, BassFlags.MixerChanPause);
            Bass.ChannelSetPosition(channel.Handle, 0); // resets position and also flushes buffer
        }

        public PlaybackState ChannelIsActive(IBassAudioChannel channel)
        {
            var state = Bass.ChannelIsActive(channel.Handle);

            // The channel may be playing meanwhile the mixer "channel" is paused.
            if (state == PlaybackState.Playing)
                state = BassMix.ChannelHasFlag(channel.Handle, BassFlags.MixerChanPause) ? PlaybackState.Paused : state;

            return state;
        }

        long IBassAudioMixer.GetChannelPosition(IBassAudioChannel channel, PositionFlags mode) => BassMix.ChannelGetPosition(channel.Handle);

        bool IBassAudioMixer.SetChannelPosition(IBassAudioChannel channel, long pos, PositionFlags mode) => BassMix.ChannelSetPosition(channel.Handle, pos, mode);

        public void UpdateDevice(int deviceIndex)
        {
            if (mixerHandle == 0)
            {
                mixerHandle = BassMix.CreateMixerStream(frequency, 2, BassFlags.MixerNonStop | BassFlags.Float);

                // Register all channels that have an active handle, which were added to the mixer prior to it being loaded.
                foreach (var channel in mixedChannels)
                {
                    if (channel.Handle != 0)
                        ((IBassAudioMixer)this).RegisterChannel(channel);
                }
            }
            else
                Bass.ChannelSetDevice(mixerHandle, deviceIndex);

            Bass.ChannelPlay(mixerHandle);
        }

        protected override void UpdateState()
        {
            FrameStatistics.Add(StatisticsCounterType.MixChannels, mixedChannels.Count);
            base.UpdateState();
        }
    }
}
