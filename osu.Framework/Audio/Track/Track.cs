// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using osu.Framework.Timing;
using System;
using osu.Framework.Bindables;

namespace osu.Framework.Audio.Track
{
    public abstract class Track : AdjustableAudioComponent, IAdjustableClock, IHasTempoAdjust, ITrack
    {
        public event Action Completed;
        public event Action Failed;

        protected virtual void RaiseCompleted() => Completed?.Invoke();
        protected virtual void RaiseFailed() => Failed?.Invoke();

        /// <summary>
        /// Is this track capable of producing audio?
        /// </summary>
        public virtual bool IsDummyDevice => true;

        /// <summary>
        /// Point in time in milliseconds to restart the track to on loop or <see cref="Restart"/>.
        /// </summary>
        public double RestartPoint { get; set; }

        /// <summary>
        /// The speed of track playback. Does not affect pitch, but will reduce playback quality due to skipped frames.
        /// </summary>
        public readonly BindableDouble Tempo = new BindableDouble(1);

        protected Track()
        {
            Tempo.ValueChanged += InvalidateState;
        }

        protected override void OnLooping() => Restart();

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
            Frequency.Value = 1;
            Tempo.Value = 1;
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
            get => Frequency.Value * Tempo.Value;
            set => throw new InvalidOperationException($"Setting {nameof(Rate)} directly on a {nameof(Track)} is not supported. Set {nameof(IHasPitchAdjust.PitchAdjust)} or {nameof(IHasTempoAdjust.TempoAdjust)} instead.");
        }

        public bool IsReversed => Rate < 0;

        public override bool HasCompleted => IsLoaded && !IsRunning && CurrentTime >= Length;

        /// <summary>
        /// Current amplitude of stereo channels where 1 is full volume and 0 is silent.
        /// LeftChannel and RightChannel represent the maximum current amplitude of all of the left and right channels respectively.
        /// The most recent values are returned. Synchronisation between channels should not be expected.
        /// </summary>
        public virtual TrackAmplitudes CurrentAmplitudes => new TrackAmplitudes();

        /// <summary>
        /// The playback tempo multiplier for this track, where 1 is the original speed.
        /// </summary>
        public double TempoAdjust
        {
            get => Tempo.Value;
            set => Tempo.Value = value;
        }

        protected override void UpdateState()
        {
            FrameStatistics.Increment(StatisticsCounterType.Tracks);
            base.UpdateState();
        }
    }
}
