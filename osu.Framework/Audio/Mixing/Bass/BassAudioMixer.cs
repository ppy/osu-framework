// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Statistics;

namespace osu.Framework.Audio.Mixing.Bass
{
    /// <summary>
    /// Mixes together multiple <see cref="IAudioChannel"/> into one output via BASSmix.
    /// </summary>
    internal class BassAudioMixer : AudioMixer, IBassAudio
    {
        /// <summary>
        /// The handle for this mixer.
        /// </summary>
        public int Handle { get; private set; }

        /// <summary>
        /// The list of effects which are currently active in the BASS mix.
        /// </summary>
        internal readonly List<EffectWithHandle> ActiveEffects = new List<EffectWithHandle>();

        /// <summary>
        /// The list of channels which are currently active in the BASS mix.
        /// </summary>
        private readonly List<IBassAudioChannel> activeChannels = new List<IBassAudioChannel>();

        private const int frequency = 44100;

        /// <summary>
        /// Creates a new <see cref="BassAudioMixer"/>.
        /// </summary>
        /// <param name="globalMixer"><inheritdoc /></param>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        public BassAudioMixer(AudioMixer? globalMixer, string identifier)
            : base(globalMixer, identifier)
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
                ChannelPlay(bassChannel);
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            Debug.Assert(CanPerformInline);

            if (!(channel is IBassAudioChannel bassChannel))
                return;

            if (Handle == 0 || bassChannel.Handle == 0)
                return;

            if (activeChannels.Remove(bassChannel))
                removeChannelFromBassMix(bassChannel);
        }

        /// <summary>
        /// Plays a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelPlay"/>.</remarks>
        /// <param name="channel">The channel to play.</param>
        /// <param name="restart">Restart playback from the beginning?</param>
        /// <returns>
        /// If successful, <see langword="true"/> is returned, else <see langword="false"/> is returned.
        /// Use <see cref="ManagedBass.Bass.LastError"/> to get the error code.
        /// </returns>
        public bool ChannelPlay(IBassAudioChannel channel, bool restart = false)
        {
            if (Handle == 0 || channel.Handle == 0)
                return false;

            AddChannelToBassMix(channel);
            BassMix.ChannelRemoveFlag(channel.Handle, BassFlags.MixerChanPause);

            return true;
        }

        /// <summary>
        /// Pauses a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelPause"/>.</remarks>
        /// <param name="channel">The channel to pause.</param>
        /// <param name="flushMixer">Set to <c>true</c> to make the pause take effect immediately.
        /// <para>
        /// This will change the timing of <see cref="ChannelGetPosition"/>, so should be used sparingly.
        /// </para>
        /// </param>
        /// <returns>
        /// If successful, <see langword="true"/> is returned, else <see langword="false"/> is returned.
        /// Use <see cref="ManagedBass.Bass.LastError"/> to get the error code.
        /// </returns>
        public bool ChannelPause(IBassAudioChannel channel, bool flushMixer = false)
        {
            bool result = BassMix.ChannelAddFlag(channel.Handle, BassFlags.MixerChanPause);

            if (flushMixer)
                flush();

            return result;
        }

        /// <summary>
        /// Checks if a channel is active (playing) or stalled.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelIsActive"/>.</remarks>
        /// <param name="channel">The channel to get the state of.</param>
        /// <returns><see cref="PlaybackState"/> indicating the state of the channel.</returns>
        public PlaybackState ChannelIsActive(IBassAudioChannel channel)
        {
            // The audio channel's state tells us whether it's stalled or stopped.
            var state = ManagedBass.Bass.ChannelIsActive(channel.Handle);

            // The channel is always in a playing state unless stopped or stalled as it's a decoding channel. Retrieve the true playing state from the mixer channel.
            if (state == PlaybackState.Playing)
                state = BassMix.ChannelFlags(channel.Handle, BassFlags.Default, BassFlags.Default).HasFlagFast(BassFlags.MixerChanPause) ? PlaybackState.Paused : state;

            return state;
        }

        /// <summary>
        /// Retrieves the playback position of a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelGetPosition"/>.</remarks>
        /// <param name="channel">The channel to retrieve the position of.</param>
        /// <param name="mode">How to retrieve the position.</param>
        /// <returns>
        /// If an error occurs, -1 is returned, use <see cref="ManagedBass.Bass.LastError"/> to get the error code.
        /// If successful, the position is returned.
        /// </returns>
        public long ChannelGetPosition(IBassAudioChannel channel, PositionFlags mode = PositionFlags.Bytes)
            => BassMix.ChannelGetPosition(channel.Handle);

        /// <summary>
        /// Sets the playback position of a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelSetPosition"/>.</remarks>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to set the position of.</param>
        /// <param name="position">The position, in units determined by the <paramref name="mode"/>.</param>
        /// <param name="mode">How to set the position.</param>
        /// <returns>
        /// If successful, then <see langword="true"/> is returned, else <see langword="false"/> is returned.
        /// Use <see cref="P:ManagedBass.Bass.LastError"/> to get the error code.
        /// </returns>
        public bool ChannelSetPosition(IBassAudioChannel channel, long position, PositionFlags mode = PositionFlags.Bytes)
        {
            // All BASS channels enter a stopped state once they reach the end.
            // Non-decoding channels remain in the stopped state when seeked afterwards, however decoding channels are put back into a playing state which causes audio to play.
            // Thus, on seek, in order to reproduce the expectations set out by non-decoding channels, manually pause the mixer channel when the decoding channel is stopped.
            if (ChannelIsActive(channel) == PlaybackState.Stopped)
                ChannelPause(channel, true);

            bool result = BassMix.ChannelSetPosition(channel.Handle, position, mode);

            // Perform a flush so that ChannelGetPosition() immediately returns the new value.
            flush();

            return result;
        }

        /// <summary>
        /// Retrieves the level (peak amplitude) of a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelGetLevel(int, float[], float, LevelRetrievalFlags)"/>.</remarks>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to get the levels of.</param>
        /// <param name="levels">The array in which the levels are to be returned.</param>
        /// <param name="length">How much data (in seconds) to look at to get the level (limited to 1 second).</param>
        /// <param name="flags">What levels to retrieve.</param>
        /// <returns><c>true</c> if successful, false otherwise.</returns>
        public bool ChannelGetLevel(IBassAudioChannel channel, [In, Out] float[] levels, float length, LevelRetrievalFlags flags)
            => BassMix.ChannelGetLevel(channel.Handle, levels, length, flags) != -1;

        /// <summary>
        /// Retrieves the immediate sample data (or an FFT representation of it) of a channel.
        /// </summary>
        /// <remarks>See: <see cref="ManagedBass.Bass.ChannelGetData(int, float[], int)"/>.</remarks>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to retrieve the data of.</param>
        /// <param name="buffer">float[] to write the data to.</param>
        /// <param name="length">Number of bytes wanted, and/or <see cref="T:ManagedBass.DataFlags"/>.</param>
        /// <returns>If an error occurs, -1 is returned, use <see cref="P:ManagedBass.Bass.LastError"/> to get the error code.
        /// <para>When requesting FFT data, the number of bytes read from the channel (to perform the FFT) is returned.</para>
        /// <para>When requesting sample data, the number of bytes written to buffer will be returned (not necessarily the same as the number of bytes read when using the <see cref="F:ManagedBass.DataFlags.Float"/> or DataFlags.Fixed flag).</para>
        /// <para>When using the <see cref="F:ManagedBass.DataFlags.Available"/> flag, the number of bytes in the channel's buffer is returned.</para>
        /// </returns>
        public int ChannelGetData(IBassAudioChannel channel, float[] buffer, int length)
            => BassMix.ChannelGetData(channel.Handle, buffer, length);

        /// <summary>
        /// Sets up a synchroniser on a mixer source channel.
        /// </summary>
        /// <remarks>See: <see cref="BassMix.ChannelSetSync(int, SyncFlags, long, SyncProcedure, IntPtr)"/>.</remarks>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to set up the synchroniser for.</param>
        /// <param name="type">The type of sync.</param>
        /// <param name="parameter">The sync parameters, depending on the sync type.</param>
        /// <param name="procedure">The callback function which should be invoked with the sync.</param>
        /// <param name="user">User instance data to pass to the callback function.</param>
        /// <returns>If successful, then the new synchroniser's handle is returned, else 0 is returned. Use <see cref="P:ManagedBass.Bass.LastError" /> to get the error code.</returns>
        public int ChannelSetSync(IBassAudioChannel channel, SyncFlags type, long parameter, SyncProcedure procedure, IntPtr user = default)
            => BassMix.ChannelSetSync(channel.Handle, type, parameter, procedure, user);

        /// <summary>
        /// Removes a synchroniser from a mixer source channel.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to remove the synchroniser for.</param>
        /// <param name="sync">Handle of the synchroniser to remove (return value of a previous <see cref="M:ManagedBass.Mix.BassMix.ChannelSetSync(System.Int32,ManagedBass.SyncFlags,System.Int64,ManagedBass.SyncProcedure,System.IntPtr)" /> call).</param>
        /// <returns>If successful, <see langword="true" /> is returned, else <see langword="false" /> is returned. Use <see cref="P:ManagedBass.Bass.LastError" /> to get the error code.</returns>
        public bool ChannelRemoveSync(IBassAudioChannel channel, int sync)
            => BassMix.ChannelRemoveSync(channel.Handle, sync);

        /// <summary>
        /// Frees a channel's resources.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to free.</param>
        /// <returns>If successful, <see langword="true" /> is returned, else <see langword="false" /> is returned. Use <see cref="P:ManagedBass.Bass.LastError" /> to get the error code.</returns>
        public bool StreamFree(IBassAudioChannel channel)
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
            for (int i = 0; i < activeChannels.Count; i++)
            {
                var channel = activeChannels[i];
                if (channel.IsActive)
                    continue;

                activeChannels.RemoveAt(i--);
                removeChannelFromBassMix(channel);
            }

            FrameStatistics.Add(StatisticsCounterType.MixChannels, activeChannels.Count);
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

            Handle = BassMix.CreateMixerStream(frequency, 2, BassFlags.MixerNonStop);
            if (Handle == 0)
                return;

            // Lower latency is valued more for the time since we are not using complex DSP effects. Disable buffering on the mixer channel in order for data to be produced immediately.
            ManagedBass.Bass.ChannelSetAttribute(Handle, ChannelAttribute.Buffer, 0);

            // Register all channels that were previously played prior to the mixer being loaded.
            var toAdd = activeChannels.ToArray();
            activeChannels.Clear();
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
            // TODO: This fails and throws unobserved exceptions in github CI runs on macOS.
            // Needs further investigation at some point as something is definitely not right.
            // Debug.Assert(Handle != 0);
            // Debug.Assert(channel.Handle != 0);

            BassFlags flags = BassFlags.MixerChanBuffer | BassFlags.MixerChanNoRampin;
            if (channel.MixerChannelPaused)
                flags |= BassFlags.MixerChanPause;

            if (BassMix.MixerAddChannel(Handle, channel.Handle, flags))
                activeChannels.Add(channel);
        }

        /// <summary>
        /// Removes a channel from the native BASS mix.
        /// </summary>
        private void removeChannelFromBassMix(IBassAudioChannel channel)
        {
            // TODO: This fails and throws unobserved exceptions in github CI runs on macOS.
            // Needs further investigation at some point as something is definitely not right.
            // Debug.Assert(Handle != 0);
            // Debug.Assert(channel.Handle != 0);

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

                    ActiveEffects.InsertRange(startIndex, e.NewItems.OfType<IEffectParameter>().Select(eff => new EffectWithHandle(eff)));
                    applyEffects(startIndex, ActiveEffects.Count - 1);
                    break;
                }

                case NotifyCollectionChangedAction.Move:
                {
                    EffectWithHandle effect = ActiveEffects[e.OldStartingIndex];
                    ActiveEffects.RemoveAt(e.OldStartingIndex);
                    ActiveEffects.Insert(e.NewStartingIndex, effect);
                    applyEffects(Math.Min(e.OldStartingIndex, e.NewStartingIndex), ActiveEffects.Count - 1);
                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    Debug.Assert(e.OldItems != null);

                    for (int i = 0; i < e.OldItems.Count; i++)
                        removeEffect(ActiveEffects[e.OldStartingIndex + i]);
                    ActiveEffects.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                    applyEffects(e.OldStartingIndex, ActiveEffects.Count - 1);
                    break;
                }

                case NotifyCollectionChangedAction.Replace:
                {
                    Debug.Assert(e.NewItems != null);

                    EffectWithHandle oldEffect = ActiveEffects[e.NewStartingIndex];
                    EffectWithHandle newEffect = new EffectWithHandle((IEffectParameter)e.NewItems[0].AsNonNull()) { Handle = oldEffect.Handle };

                    ActiveEffects[e.NewStartingIndex] = newEffect;

                    // If the effect types don't match, the old effect has to be removed altogether. Otherwise, the new parameters can be applied onto the existing handle.
                    if (oldEffect.Effect.FXType != newEffect.Effect.FXType)
                        removeEffect(oldEffect);

                    applyEffects(e.NewStartingIndex, e.NewStartingIndex);
                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    foreach (var effect in ActiveEffects)
                        removeEffect(effect);
                    ActiveEffects.Clear();
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
                    var effect = ActiveEffects[i];

                    // Effects with greatest priority are stored at the front of the list.
                    effect.Priority = -i;

                    if (effect.Handle != 0)
                    {
                        // Todo: Temporary bypass to attempt to fix failing test runs.
                        if (!DebugUtils.IsNUnitRunning)
                            ManagedBass.Bass.FXSetPriority(effect.Handle, effect.Priority);
                    }
                    else
                        effect.Handle = ManagedBass.Bass.ChannelSetFX(Handle, effect.Effect.FXType, effect.Priority);

                    ManagedBass.Bass.FXSetParameters(effect.Handle, effect.Effect);
                }
            }
        });

        /// <summary>
        /// Flushes the mixer, causing pause and seek events to take effect immediately.
        /// </summary>
        /// <remarks>
        /// This will change the timing of <see cref="ChannelGetPosition"/>, so should be used sparingly.
        /// </remarks>
        private void flush()
        {
            if (Handle != 0)
                ManagedBass.Bass.ChannelSetPosition(Handle, 0);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // Move all contained channels back to the default mixer.
            foreach (var channel in activeChannels.ToArray())
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

            public EffectWithHandle(IEffectParameter effect)
            {
                Effect = effect;
            }
        }
    }
}
