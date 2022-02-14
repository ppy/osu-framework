// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using osu.Framework.Statistics;
using System;
using System.Threading.Tasks;
using osu.Framework.Audio.Mixing;

namespace osu.Framework.Audio.Track
{
    public abstract class Track : AdjustableAudioComponent, ITrack, IAudioChannel
    {
        public event Action? Completed;
        public event Action? Failed;

        protected virtual void RaiseCompleted() => Completed?.Invoke();
        protected virtual void RaiseFailed() => Failed?.Invoke();

        public virtual bool IsDummyDevice => true;

        public double RestartPoint { get; set; }

        public virtual bool Looping { get; set; }

        /// <summary>
        /// Reset this track to a logical default state.
        /// </summary>
        public virtual void Reset()
        {
            Volume.Value = 1;

            ResetSpeedAdjustments();

            Stop();
            Seek(0);
        }

        /// <summary>
        /// Restarts this track from the <see cref="RestartPoint"/> while retaining adjustments.
        /// </summary>
        public virtual void Restart()
        {
            Stop();
            Seek(RestartPoint);
            Start();
        }

        public virtual void ResetSpeedAdjustments()
        {
            RemoveAllAdjustments(AdjustableProperty.Frequency);
            RemoveAllAdjustments(AdjustableProperty.Tempo);
        }

        /// <summary>
        /// Current position in milliseconds.
        /// </summary>
        public abstract double CurrentTime { get; }

        private double length;

        /// <summary>
        /// Length of the track in milliseconds.
        /// </summary>
        public double Length
        {
            get => length;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Track length must be >= 0.", nameof(value));

                length = value;
            }
        }

        public virtual int? Bitrate => null;

        /// <summary>
        /// Seek to a new position.
        /// </summary>
        /// <param name="seek">New position in milliseconds</param>
        /// <returns>Whether the seek was successful.</returns>
        public abstract bool Seek(double seek);

        public virtual void Start()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not start disposed tracks.");
        }

        public virtual void Stop()
        {
        }

        public abstract bool IsRunning { get; }

        /// <summary>
        /// Overall playback rate (1 is 100%, -1 is reversed at 100%).
        /// </summary>
        public virtual double Rate
        {
            get => AggregateFrequency.Value * AggregateTempo.Value;
            set => throw new InvalidOperationException($"Setting {nameof(Rate)} directly on a {nameof(Track)} is not supported. Set {nameof(Tempo)} or {nameof(Frequency)} instead.");
        }

        public bool IsReversed => Rate < 0;

        public override bool HasCompleted => IsLoaded && !IsRunning && CurrentTime >= Length;

        public virtual ChannelAmplitudes CurrentAmplitudes { get; } = ChannelAmplitudes.Empty;

        protected override void UpdateState()
        {
            FrameStatistics.Increment(StatisticsCounterType.Tracks);

            base.UpdateState();

            if (Looping && HasCompleted)
                Restart();
        }

        #region Mixing

        protected virtual AudioMixer? Mixer { get; set; }

        AudioMixer? IAudioChannel.Mixer
        {
            get => Mixer;
            set => Mixer = value;
        }

        Task IAudioChannel.EnqueueAction(Action action) => EnqueueAction(action);

        #endregion
    }
}
