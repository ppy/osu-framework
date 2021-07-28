// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Bindables;
using osu.Framework.Lists;
using osu.Framework.Statistics;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Mixes together multiple <see cref="IAudioChannel"/> into one output via BASSmix.
    /// </summary>
    internal class BassAudioMixer : AudioMixer, IBassAudio, IBassAudioMixer
    {
        /// <summary>
        /// The handle for this mixer.
        /// </summary>
        public int Handle { get; private set; }

        private readonly WeakList<IBassAudioChannel> mixedChannels = new WeakList<IBassAudioChannel>();
        private readonly List<EffectWithHandle> effects = new List<EffectWithHandle>();

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

        public override BindableList<IEffectParameter> Effects { get; } = new BindableList<IEffectParameter>();

        protected override void AddInternal(IAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);

            if (!(channel is IBassAudioChannel bassChannel))
                return;

            Debug.Assert(!mixedChannels.Contains(bassChannel));
            mixedChannels.Add(bassChannel);

            if (Handle == 0 || bassChannel.Handle == 0)
                return;

            ((IBassAudioMixer)this).RegisterHandle(bassChannel);
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

            bassChannel.MixerChannelPaused = BassMix.ChannelHasFlag(bassChannel.Handle, BassFlags.MixerChanPause);
            BassMix.MixerRemoveChannel(bassChannel.Handle);
        }

        void IBassAudioMixer.RegisterHandle(IBassAudioChannel channel)
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
            BassMix.ChannelRemoveFlag(channel.Handle, BassFlags.MixerChanPause);
            return Bass.LastError == Errors.OK;
        }

        bool IBassAudioMixer.PauseChannel(IBassAudioChannel channel)
        {
            BassMix.ChannelAddFlag(channel.Handle, BassFlags.MixerChanPause);
            return Bass.LastError == Errors.OK;
        }

        void IBassAudioMixer.StopChannel(IBassAudioChannel channel)
        {
            BassMix.ChannelAddFlag(channel.Handle, BassFlags.MixerChanPause);
            Bass.ChannelSetPosition(channel.Handle, 0); // resets position and also flushes buffer
        }

        public PlaybackState ChannelIsActive(IBassAudioChannel channel)
        {
            // The audio channel's state tells us whether it's stalled or stopped.
            var state = Bass.ChannelIsActive(channel.Handle);

            // The channel is always in a playing state unless stopped or stalled as it's a decoding channel. Retrieve the true playing state from the mixer channel.
            if (state == PlaybackState.Playing)
                state = BassMix.ChannelHasFlag(channel.Handle, BassFlags.MixerChanPause) ? PlaybackState.Paused : state;

            return state;
        }

        long IBassAudioMixer.GetChannelPosition(IBassAudioChannel channel, PositionFlags mode) => BassMix.ChannelGetPosition(channel.Handle);

        bool IBassAudioMixer.SetChannelPosition(IBassAudioChannel channel, long pos, PositionFlags mode) => BassMix.ChannelSetPosition(channel.Handle, pos, mode);

        void IBassAudioMixer.UnregisterHandle(IBassAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);
            Debug.Assert(channel.Handle != 0);

            Remove(channel, false);
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
                    ((IBassAudioMixer)this).RegisterHandle(channel);
            }

            Effects.BindCollectionChanged(onEffectsChanged, true);

            Bass.ChannelPlay(Handle);
        }

        private void onEffectsChanged(object? sender, NotifyCollectionChangedEventArgs e) => EnqueueAction(() =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    Debug.Assert(e.NewItems != null);

                    // Work around BindableList sending initial event start with index -1.
                    int startIndex = Math.Max(0, e.NewStartingIndex);

                    effects.InsertRange(startIndex, e.NewItems.OfType<IEffectParameter>().Select(eff => new EffectWithHandle(eff)));
                    reapplyEffects(startIndex, effects.Count - 1);
                    break;
                }

                case NotifyCollectionChangedAction.Move:
                {
                    EffectWithHandle effect = effects[e.OldStartingIndex];
                    effects.RemoveAt(e.OldStartingIndex);
                    effects.Insert(e.NewStartingIndex, effect);
                    reapplyEffects(Math.Min(e.OldStartingIndex, e.NewStartingIndex), effects.Count - 1);
                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    Debug.Assert(e.OldItems != null);

                    effects.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                    reapplyEffects(e.OldStartingIndex, effects.Count - 1);
                    break;
                }

                case NotifyCollectionChangedAction.Replace:
                {
                    Debug.Assert(e.NewItems != null);

                    EffectWithHandle oldEffect = effects[e.NewStartingIndex];
                    effects[e.NewStartingIndex] = new EffectWithHandle((IEffectParameter?)e.NewItems[0]);
                    removeEffect(oldEffect);
                    reapplyEffects(e.NewStartingIndex, e.NewStartingIndex);
                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    foreach (var effect in effects)
                        removeEffect(effect);
                    effects.Clear();
                    break;
                }
            }

            void removeEffect(EffectWithHandle effect)
            {
                if (effect.Handle != 0)
                    Bass.ChannelRemoveFX(Handle, effect.Handle);
            }

            void reapplyEffects(int startIndex, int endIndex)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    var effect = effects[i];

                    // Remove any existing effect (priority could have changed).
                    removeEffect(effect);

                    effect.Handle = Bass.ChannelSetFX(Handle, effect.Effect.FXType, i);
                    Bass.FXSetParameters(effect.Handle, effect.Effect);
                }
            }
        });

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

        private class EffectWithHandle
        {
            public int Handle { get; set; }

            public readonly IEffectParameter Effect;

            public EffectWithHandle(IEffectParameter? effect)
            {
                Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            }
        }
    }
}
