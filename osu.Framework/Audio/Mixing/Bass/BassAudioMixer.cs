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
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Statistics;

namespace osu.Framework.Audio.Mixing.Bass
{
    /// <summary>
    /// Mixes together multiple <see cref="IAudioChannel"/> into one output via BASSmix.
    /// </summary>
    internal class BassAudioMixer : AudioMixer, IBassAudioChannelInterface, IBassAudio
    {
        /// <summary>
        /// The handle for this mixer.
        /// </summary>
        public int Handle { get; private set; }

        internal readonly List<EffectWithHandle> MixedEffects = new List<EffectWithHandle>();
        private readonly List<IBassAudioChannel> mixedChannels = new List<IBassAudioChannel>();

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

            if (Handle == 0 || bassChannel.Handle == 0)
                return;

            if (!bassChannel.MixerChannelPaused)
                ((IBassAudioChannelInterface)this).ChannelPlay(bassChannel);
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);

            if (!(channel is IBassAudioChannel bassChannel))
                return;

            if (Handle == 0 || bassChannel.Handle == 0)
                return;

            if (mixedChannels.Remove(bassChannel))
                removeChannelFromBassMix(bassChannel);
        }

        public bool ChannelPlay(IBassAudioChannel channel, bool restart)
        {
            if (Handle == 0 || channel.Handle == 0)
                return false;

            AddChannelToBassMix(channel);
            BassMix.ChannelRemoveFlag(channel.Handle, BassFlags.MixerChanPause);

            return true;
        }

        bool IBassAudioChannelInterface.ChannelPause(IBassAudioChannel channel) => BassMix.ChannelAddFlag(channel.Handle, BassFlags.MixerChanPause);

        bool IBassAudioChannelInterface.ChannelStop(IBassAudioChannel channel)
        {
            BassMix.ChannelAddFlag(channel.Handle, BassFlags.MixerChanPause);
            return ManagedBass.Bass.ChannelSetPosition(channel.Handle, 0); // resets position and also flushes buffer
        }

        PlaybackState IBassAudioChannelInterface.ChannelIsActive(IBassAudioChannel channel)
        {
            // The audio channel's state tells us whether it's stalled or stopped.
            var state = ManagedBass.Bass.ChannelIsActive(channel.Handle);

            // The channel is always in a playing state unless stopped or stalled as it's a decoding channel. Retrieve the true playing state from the mixer channel.
            if (state == PlaybackState.Playing)
                state = BassMix.ChannelHasFlag(channel.Handle, BassFlags.MixerChanPause) ? PlaybackState.Paused : state;

            return state;
        }

        long IBassAudioChannelInterface.ChannelGetPosition(IBassAudioChannel channel, PositionFlags mode)
            => BassMix.ChannelGetPosition(channel.Handle);

        bool IBassAudioChannelInterface.ChannelSetPosition(IBassAudioChannel channel, long position, PositionFlags mode)
            => BassMix.ChannelSetPosition(channel.Handle, position, mode);

        bool IBassAudioChannelInterface.ChannelGetLevel(IBassAudioChannel channel, float[] levels, float length, LevelRetrievalFlags flags)
            => BassMix.ChannelGetLevel(channel.Handle, levels, length, flags) != -1;

        int IBassAudioChannelInterface.ChannelGetData(IBassAudioChannel channel, float[] buffer, int length)
            => BassMix.ChannelGetData(channel.Handle, buffer, length);

        bool IBassAudioChannelInterface.StreamFree(IBassAudioChannel channel)
        {
            Remove(channel, false);
            return ManagedBass.Bass.StreamFree(channel.Handle);
        }

        public void UpdateDevice(int deviceIndex)
        {
            if (Handle == 0)
                createMixer();
            else
                ManagedBass.Bass.ChannelSetDevice(Handle, deviceIndex);
        }

        protected override void UpdateState()
        {
            for (int i = 0; i < mixedChannels.Count; i++)
            {
                var channel = mixedChannels[i];
                if (channel.IsActive)
                    continue;

                mixedChannels.RemoveAt(i--);
                removeChannelFromBassMix(channel);
            }

            FrameStatistics.Add(StatisticsCounterType.MixChannels, mixedChannels.Count);
            base.UpdateState();
        }

        private void createMixer()
        {
            if (Handle != 0)
                return;

            // Make sure that bass is initialised before trying to create a mixer.
            // If not, this will be called again when the device is initialised via UpdateDevice().
            if (!ManagedBass.Bass.GetDeviceInfo(ManagedBass.Bass.CurrentDevice, out var deviceInfo) || !deviceInfo.IsInitialized)
                return;

            Handle = BassMix.CreateMixerStream(frequency, 2, BassFlags.MixerNonStop | BassFlags.Float);
            if (Handle == 0)
                return;

            // Register all channels that were previously played prior to the mixer being loaded.
            var toAdd = mixedChannels.ToArray();
            mixedChannels.Clear();
            foreach (var channel in toAdd)
                AddChannelToBassMix(channel);

            Effects.BindCollectionChanged(onEffectsChanged, true);

            ManagedBass.Bass.ChannelPlay(Handle);
        }

        /// <summary>
        /// Adds a channel to the native BASS mix.
        /// </summary>
        public void AddChannelToBassMix(IBassAudioChannel channel)
        {
            Debug.Assert(Handle != 0);
            Debug.Assert(channel.Handle != 0);

            BassFlags flags = BassFlags.MixerChanBuffer;
            if (channel.MixerChannelPaused)
                flags |= BassFlags.MixerChanPause;

            if (BassMix.MixerAddChannel(Handle, channel.Handle, flags))
                mixedChannels.Add(channel);
        }

        /// <summary>
        /// Removes a channel from the native BASS mix.
        /// </summary>
        private void removeChannelFromBassMix(IBassAudioChannel channel)
        {
            Debug.Assert(Handle != 0);
            Debug.Assert(channel.Handle != 0);

            channel.MixerChannelPaused = BassMix.ChannelHasFlag(channel.Handle, BassFlags.MixerChanPause);
            BassMix.MixerRemoveChannel(channel.Handle);
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

                    MixedEffects.InsertRange(startIndex, e.NewItems.OfType<IEffectParameter>().Select(eff => new EffectWithHandle(eff)));
                    applyEffects(startIndex, MixedEffects.Count - 1);
                    break;
                }

                case NotifyCollectionChangedAction.Move:
                {
                    EffectWithHandle effect = MixedEffects[e.OldStartingIndex];
                    MixedEffects.RemoveAt(e.OldStartingIndex);
                    MixedEffects.Insert(e.NewStartingIndex, effect);
                    applyEffects(Math.Min(e.OldStartingIndex, e.NewStartingIndex), MixedEffects.Count - 1);
                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    Debug.Assert(e.OldItems != null);

                    for (int i = 0; i < e.OldItems.Count; i++)
                        removeEffect(MixedEffects[e.OldStartingIndex + i]);
                    MixedEffects.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                    applyEffects(e.OldStartingIndex, MixedEffects.Count - 1);
                    break;
                }

                case NotifyCollectionChangedAction.Replace:
                {
                    Debug.Assert(e.NewItems != null);

                    EffectWithHandle oldEffect = MixedEffects[e.NewStartingIndex];
                    MixedEffects[e.NewStartingIndex] = new EffectWithHandle((IEffectParameter)e.NewItems[0].AsNonNull());
                    removeEffect(oldEffect);
                    applyEffects(e.NewStartingIndex, e.NewStartingIndex);
                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    foreach (var effect in MixedEffects)
                        removeEffect(effect);
                    MixedEffects.Clear();
                    break;
                }
            }

            void removeEffect(EffectWithHandle effect)
            {
                Debug.Assert(effect.Handle != 0);

                ManagedBass.Bass.ChannelRemoveFX(Handle, effect.Handle);
                effect.Handle = 0;
            }

            void applyEffects(int startIndex, int endIndex)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    var effect = MixedEffects[i];

                    // Effects with greatest priority are stored at the front of the list.
                    effect.Priority = -i;

                    if (effect.Handle != 0)
                        ManagedBass.Bass.FXSetPriority(effect.Handle, effect.Priority);
                    else
                    {
                        effect.Handle = ManagedBass.Bass.ChannelSetFX(Handle, effect.Effect.FXType, effect.Priority);
                        ManagedBass.Bass.FXSetParameters(effect.Handle, effect.Effect);
                    }
                }
            }
        });

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // Move all contained channels back to the default mixer.
            foreach (var channel in mixedChannels.ToArray())
                Remove(channel);

            if (Handle != 0)
            {
                ManagedBass.Bass.StreamFree(Handle);
                Handle = 0;
            }
        }

        internal class EffectWithHandle
        {
            public int Handle { get; set; }
            public int Priority { get; set; }

            public readonly IEffectParameter Effect;

            public EffectWithHandle(IEffectParameter? effect)
            {
                Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            }
        }
    }
}
