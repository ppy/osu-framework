// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using mysoundlib_cs;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;
using osu.Framework.Statistics;

namespace osu.Framework.Audio.Mixing.SDL3
{
    /// <summary>
    /// Mixes <see cref="ISDL3AudioChannel"/> instances and applies effects on top of them.
    /// </summary>
    internal class SDL3AudioMixer : AudioMixer
    {
        public IntPtr Handle { get; private set; }

        private readonly HashSet<ISDL3AudioChannel> activeChannels = new HashSet<ISDL3AudioChannel>(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Creates a new <see cref="SDL3AudioMixer"/>
        /// </summary>
        /// <param name="globalMixer"><inheritdoc /></param>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        public SDL3AudioMixer(AudioMixer? globalMixer, string identifier)
            : base(globalMixer, identifier)
        {
            Handle = MySoundLibrary.mslCreateMixer().ThrowIfNull();
        }

        protected override void AddInternal(IAudioChannel channel)
        {
            if (!IsDisposed && channel is ISDL3AudioChannel sdl3Channel && activeChannels.Add(sdl3Channel))
            {
                if (sdl3Channel is TrackSDL3)
                    MySoundLibrary.mslMixerAddTrack(Handle, sdl3Channel.Handle.ThrowIfNull(), 0);
                else if (sdl3Channel is SampleChannelSDL3)
                    MySoundLibrary.mslMixerAddSample(Handle, sdl3Channel.Handle.ThrowIfNull(), 0);
            }
        }

        protected override void RemoveInternal(IAudioChannel channel)
        {
            if (!IsDisposed && channel is ISDL3AudioChannel sdl3Channel && activeChannels.Remove(sdl3Channel))
            {
                if (sdl3Channel is TrackSDL3)
                    MySoundLibrary.mslMixerRemoveTrack(Handle, sdl3Channel.Handle);
                else if (sdl3Channel is SampleChannelSDL3)
                    MySoundLibrary.mslMixerRemoveSample(Handle, sdl3Channel.Handle);
            }
        }

        public void RemoveChannel(ISDL3AudioChannel channel) => Remove(channel, false);

        protected override void UpdateState()
        {
            FrameStatistics.Add(StatisticsCounterType.MixChannels, activeChannels.Count);
            base.UpdateState();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            var copy = activeChannels.ToArray();

            foreach (var channel in copy)
                Remove(channel);

            base.Dispose(disposing);

            MySoundLibrary.mslDestroyMixer(Handle);
            Handle = IntPtr.Zero;
        }

        public override AudioEffect GetNewEffect(int priority = 0) => new SDL3AudioEffect(act => EnqueueAction(act), this, priority);
    }
}
