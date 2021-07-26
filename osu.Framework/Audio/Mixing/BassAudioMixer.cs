// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Lists;
using osu.Framework.Statistics;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// A BASSmix audio mixer.
    /// </summary>
    internal class BassAudioMixer : AudioMixer, IBassAudio, IBassAudioMixer
    {
        /// <summary>
        /// The handle for this mixer.
        /// </summary>
        public int Handle { get; private set; }

        private readonly WeakList<IBassAudioChannel> mixedChannels = new WeakList<IBassAudioChannel>();
        private readonly List<EffectWithPriority> effects = new List<EffectWithPriority>();

        private const int frequency = 44100;

        /// <summary>
        /// Creates a new <see cref="BassAudioMixer"/>.
        /// </summary>
        /// <param name="defaultMixer"><inheritdoc /></param>
        public BassAudioMixer(AudioMixer? defaultMixer)
            : base(defaultMixer)
        {
            EnqueueAction(createMixer);
        }

        public override void AddEffect(IEffectParameter effect, int priority)
        {
            EnqueueAction(() =>
            {
                var effectWithPriority = new EffectWithPriority(effect, priority);
                effects.Add(effectWithPriority);
                applyEffect(effectWithPriority);
            });
        }

        public override void RemoveEffect(IEffectParameter effect)
        {
            EnqueueAction(() =>
            {
                var foundIndex = effects.FindIndex(e => e.Effect == effect);
                if (foundIndex == -1)
                    return;

                var effectWithPriority = effects[foundIndex];
                effects.RemoveAt(foundIndex);

                if (effectWithPriority.Handle == 0)
                    return;

                Bass.ChannelRemoveFX(Handle, effectWithPriority.Handle);
            });
        }

        protected override void AddInternal(IAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);

            if (!(channel is IBassAudioChannel bassChannel))
                return;

            Debug.Assert(!mixedChannels.Contains(bassChannel));
            mixedChannels.Add(bassChannel);

            if (Handle == 0 || bassChannel.Handle == 0)
                return;

            ((IBassAudioMixer)this).RegisterChannel(bassChannel);
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);

            if (!(channel is IBassAudioChannel bassChannel))
                return;

            if (!mixedChannels.Remove(bassChannel))
                return;

            if (Handle == 0 || bassChannel.Handle == 0)
                return;

            bassChannel.MixerChannelPaused = Bass.ChannelHasFlag(bassChannel.Handle, BassFlags.MixerChanPause);
            BassMix.MixerRemoveChannel(bassChannel.Handle);
        }

        void IBassAudioMixer.RegisterChannel(IBassAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);
            Debug.Assert(channel.Handle != 0);

            if (Handle == 0)
                return;

            if (!mixedChannels.Contains(channel))
                throw new InvalidOperationException("Channel needs to be added to the mixer first.");

            BassFlags flags = BassFlags.MixerChanBuffer;
            if (channel.MixerChannelPaused)
                flags |= BassFlags.MixerChanPause;

            BassMix.MixerAddChannel(Handle, channel.Handle, flags);
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

        public void StreamFree(IBassAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);
            Debug.Assert(channel.Handle != 0);

            Remove(channel, false);
            Bass.StreamFree(channel.Handle);
        }

        public void UpdateDevice(int deviceIndex)
        {
            if (Handle == 0)
                createMixer();
            else
                Bass.ChannelSetDevice(Handle, deviceIndex);
        }

        private void createMixer()
        {
            if (Handle != 0)
                return;

            // Make sure that bass is initialised before trying to create a mixer.
            // If not, this will be called again when the device is initialised via UpdateDevice().
            if (!Bass.GetDeviceInfo(Bass.CurrentDevice, out var deviceInfo) || !deviceInfo.IsInitialized)
                return;

            Handle = BassMix.CreateMixerStream(frequency, 2, BassFlags.MixerNonStop | BassFlags.Float);
            if (Handle == 0)
                return;

            // Register all channels that have an active handle, which were added to the mixer prior to it being loaded.
            foreach (var channel in mixedChannels)
            {
                if (channel.Handle != 0)
                    ((IBassAudioMixer)this).RegisterChannel(channel);
            }

            foreach (var effect in effects)
                applyEffect(effect);

            Bass.ChannelPlay(Handle);
        }

        private void applyEffect(EffectWithPriority effectWithPriority)
        {
            Debug.Assert(CanPerformInline);
            Debug.Assert(effectWithPriority.Handle == 0);

            if (Handle == 0)
                return;

            effectWithPriority.Handle = Bass.ChannelSetFX(Handle, effectWithPriority.Effect.FXType, effectWithPriority.Priority);
            Bass.FXSetParameters(effectWithPriority.Handle, effectWithPriority.Effect);
        }

        protected override void UpdateState()
        {
            FrameStatistics.Add(StatisticsCounterType.MixChannels, mixedChannels.Count());
            base.UpdateState();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // Move all contained channels back to the default mixer.
            foreach (var channel in mixedChannels.ToArray())
                Remove(channel);

            if (Handle != 0)
            {
                Bass.StreamFree(Handle);
                Handle = 0;
            }
        }

        private class EffectWithPriority : IComparable<EffectWithPriority>
        {
            public int Handle { get; set; }

            public readonly IEffectParameter Effect;
            public readonly int Priority;

            public EffectWithPriority(IEffectParameter effect, int priority)
            {
                Effect = effect;
                Priority = priority;
            }

            public int CompareTo(EffectWithPriority? other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;

                return Priority.CompareTo(other.Priority);
            }
        }
    }
}
