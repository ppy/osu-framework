// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.Statistics;
using osu.Framework.Timing;
using System;

namespace osu.Framework.Audio.Track
{
    public abstract class Track : AdjustableAudioComponent, IAdjustableClock
    {
        /// <summary>
        /// Is this track capable of producing audio?
        /// </summary>
        public virtual bool IsDummyDevice => true;

        /// <summary>
        /// States if this track should repeat.
        /// </summary>
        public bool Looping { get; set; }

        /// <summary>
        /// The speed of track playback. Does not affect pitch, but will reduce playback quality due to skipped frames.
        /// </summary>
        public readonly BindableDouble Tempo = new BindableDouble(1);

        protected Track()
        {
            Tempo.ValueChanged += InvalidateState;
        }

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
        /// Restarts this track from the beginning while retaining adjustments.
        /// </summary>
        public virtual void Restart()
        {
            Stop();
            Seek(0);
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
            get { return Frequency * Tempo; }
            set { Tempo.Value = value; }
        }

        public bool IsReversed => Rate < 0;

        public override bool HasCompleted => IsLoaded && !IsRunning && CurrentTime >= Length;

        /// <summary>
        /// Current amplitude of stereo channels where 1 is full volume and 0 is silent.
        /// LeftChannel and RightChannel represent the maximum current amplitude of all of the left and right channels respectively.
        /// The most recent values are returned. Synchronisation between channels should not be expected.
        /// </summary>
        public virtual TrackAmplitudes CurrentAmplitudes => new TrackAmplitudes();

        protected override void UpdateState()
        {
            FrameStatistics.Increment(StatisticsCounterType.Tracks);

            if (Looping && HasCompleted)
                Restart();

            base.UpdateState();
        }
    }
}
