// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Timing;
using OpenTK;

namespace osu.Framework.Audio.Track
{
    public class TrackVirtual : Track
    {
        private readonly StopwatchClock clock = new StopwatchClock();

        private double seekOffset;

        public TrackVirtual()
        {
            Length = double.PositiveInfinity;
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

            seekOffset = MathHelper.Clamp(seekOffset, 0, Length);

            return current != seekOffset;
        }

        public override void Start()
        {
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
                lock (clock) return seekOffset + clock.CurrentTime;
            }
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            lock (clock)
            {
                if (CurrentTime >= Length)
                    Stop();
            }
        }

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            lock (clock)
                clock.Rate = Tempo;
        }
    }
}
