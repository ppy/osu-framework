// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading.Tasks;
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
            seekOffset = Math.Clamp(seek, 0, Length);

            lock (clock)
            {
                if (IsRunning)
                    clock.Restart();
                else
                    clock.Reset();
            }

            return seekOffset == seek;
        }

        public override Task<bool> SeekAsync(double seek)
        {
            return Task.FromResult(Seek(seek));
        }

        public override Task StartAsync()
        {
            Start();
            return Task.CompletedTask;
        }

        public override Task StopAsync()
        {
            Stop();
            return Task.CompletedTask;
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
                    if (Looping)
                        Restart();
                    else
                    {
                        Stop();
                        RaiseCompleted();
                    }
                }
            }
        }

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            lock (clock)
                clock.Rate = Rate;
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
                Stop();

            base.Dispose(disposing);
        }
    }
}
