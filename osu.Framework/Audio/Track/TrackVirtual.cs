// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Timing;

namespace osu.Framework.Audio.Track
{
    public sealed class TrackVirtual : Track
    {
        private readonly StopwatchClock clock = new StopwatchClock();

        private double seekOffset;

        public TrackVirtual(double length)
        {
            Length = length;
        }

        public override bool Seek(double seek)
        {
            double current = CurrentTime;

            seekOffset = seek;

            lock (clock)
            {
                if (IsRunning)
                    clock.Restart();
                else
                    clock.Reset();
            }

            seekOffset = Math.Clamp(seekOffset, 0, Length);

            return current != seekOffset;
        }

        public override void Start()
        {
            if (Length == 0)
                return;

            lock (clock) clock.Start();
        }

        public override void Reset()
        {
            lock (clock) clock.Reset();
            seekOffset = 0;

            base.Reset();
        }

        public override void Stop()
        {
            lock (clock) clock.Stop();
        }

        public override bool IsRunning
        {
            get
            {
                lock (clock) return clock.IsRunning;
            }
        }

        public override double CurrentTime
        {
            get
            {
                lock (clock) return Math.Min(Length, seekOffset + clock.CurrentTime);
            }
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            lock (clock)
            {
                if (clock.IsRunning && CurrentTime >= Length)
                {
                    Stop();
                    RaiseCompleted();
                }
            }
        }

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            lock (clock)
                clock.Rate = Tempo.Value * AggregateFrequency.Value;
        }
    }
}
