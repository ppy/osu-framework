// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using osu.Framework.Audio.Mixing;
using osu.Framework.Statistics;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio.Sample
{
    public abstract class SampleChannel : AdjustableAudioComponent, ISampleChannel, IAudioChannel
    {
        internal Action<SampleChannel>? OnPlay;

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

        protected virtual AudioMixer? Mixer { get; set; }

        AudioMixer? IAudioChannel.Mixer
        {
            get => Mixer;
            set => Mixer = value;
        }

        Task IAudioChannel.EnqueueAction(Action action) => EnqueueAction(action);

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
                Stop();

            base.Dispose(disposing);
        }
    }
}
