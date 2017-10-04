// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
            ResetSpeedAdjustments();

            Stop();
            Seek(0);
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

        /// <summary>
        /// Length of the track in milliseconds.
        /// </summary>
        public double Length { get; protected set; }

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
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not stop disposed tracks.");
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

        /// <summary>
        /// Current amplitude of stereo channels where 1 is full volume and 0 is silent.
        /// LeftChannel and RightChannel represent the maximum current amplitude of all of the left and right channels respectively.
        /// The most recent values are returned. Synchronisation between channels should not be expected.
        /// </summary>
        public virtual TrackAmplitudes CurrentAmplitudes => new TrackAmplitudes();

        protected bool WaveformQueryCancelled;

        /// <summary>
        /// Constructs a <see cref="Waveform"/> from the samples in this <see cref="Track"/>.
        /// The first query will pause the audio while the waveform generates, resuming it after completion.
        /// Any subsequent queries will not pause the audio.
        /// </summary>
        /// <param name="callback">The function to be called with the generated <see cref="Waveform"/>.</param>
        public virtual void QueryWaveform(Action<Waveform> callback) => callback?.Invoke(new Waveform(null, 1, 1));

        /// <summary>
        /// Cancels a pending waveform generation query. This will not cancel a currently running waveform generation.
        /// </summary>
        public void CancelWaveformQuery() => WaveformQueryCancelled = true;

        public override void Update()
        {
            FrameStatistics.Increment(StatisticsCounterType.Tracks);

            if (Looping && HasCompleted)
            {
                Reset();
                Start();
            }

            base.Update();
        }
    }
}
