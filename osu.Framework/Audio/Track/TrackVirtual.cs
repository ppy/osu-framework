// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Timing;

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
            lock (clock) clock.Restart();

            if (Length > 0 && seekOffset > Length)
                seekOffset = Length;

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

        public override bool HasCompleted
        {
            get
            {
                lock (clock) return base.HasCompleted || IsLoaded && !IsRunning && CurrentTime >= Length;
            }
        }

        public override double CurrentTime
        {
            get
            {
                lock (clock) return seekOffset + clock.CurrentTime;
            }
        }

        public override void Update()
        {
            base.Update();

            lock (clock)
            {
                if (CurrentTime >= Length)
                    Stop();
            }
        }
    }
}
