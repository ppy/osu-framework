// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using osu.Framework.Audio.Mixing;
using osu.Framework.Statistics;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio.Sample
{
    public abstract class SampleChannel : AdjustableAudioComponent, ISampleChannel, IAudioChannel
    {
        internal event Action<SampleChannel>? OnPlay;

        /// <summary>
        /// Creates a new <see cref="SampleChannel"/>.
        /// </summary>
        /// <param name="defaultMixer">The default <see cref="AudioMixer"/> to house this <see cref="SampleChannel"/>.
        /// The <see cref="SampleChannel"/> can never be removed from this <see cref="AudioMixer"/>, but can be moved to other mixers via <see cref="AudioMixer.Add"/>.</param>
        protected SampleChannel(IAudioMixer defaultMixer)
        {
            Mixer = this.defaultMixer = defaultMixer;

            defaultMixer.Add(this);
        }

        public virtual void Play()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not play disposed sample channels.");

            Played = true;
            OnPlay?.Invoke(this);
        }

        public virtual void Stop()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
                Stop();

            base.Dispose(disposing);
        }

        protected override void UpdateState()
        {
            FrameStatistics.Increment(StatisticsCounterType.SChannels);
            base.UpdateState();
        }

        public bool Played { get; private set; }

        public abstract bool Playing { get; }

        public virtual bool Looping { get; set; }

        public override bool IsAlive => base.IsAlive && Playing;

        public virtual ChannelAmplitudes CurrentAmplitudes { get; } = ChannelAmplitudes.Empty;

        #region Mixing

        public IAudioMixer Mixer { get; private set; }

        private readonly IAudioMixer defaultMixer;

        void IAudioChannel.SetMixer(IAudioMixer? mixer) => Mixer = mixer ?? defaultMixer;

        #endregion
    }
}
