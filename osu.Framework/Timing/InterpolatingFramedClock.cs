// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A clock which uses an internal stopwatch to interpolate (smooth out) a source.
    /// Note that this will NOT function unless a source has been set.
    /// </summary>
    public class InterpolatingFramedClock : IFrameBasedClock
    {
        private readonly FramedClock clock = new FramedClock(new StopwatchClock(true));
        protected FramedClock SourceClock;
        protected double LastInterpolatedTime;
        protected double CurrentInterpolatedTime;

        public FrameTimeInfo TimeInfo => new FrameTimeInfo { Elapsed = ElapsedFrameTime, Current = CurrentTime };

        public double AverageFrameTime { get; }

        public double FramesPerSecond { get; }

        public void ChangeSource(IClock source)
        {
            SourceClock = new FramedClock(source);
            LastInterpolatedTime = 0;
            CurrentInterpolatedTime = 0;
        }

        public InterpolatingFramedClock(IClock source = null)
        {
            ChangeSource(source);
        }

        public virtual double CurrentTime => sourceIsRunning ? CurrentInterpolatedTime : SourceClock.CurrentTime;

        public double AllowableErrorMilliseconds = 1000.0 / 60 * 2;

        private bool sourceIsRunning;

        public double Rate => SourceClock.Rate;

        public virtual bool IsRunning => SourceClock.IsRunning;

        public virtual double Drift => CurrentTime - SourceClock.CurrentTime;

        public virtual double ElapsedFrameTime => CurrentInterpolatedTime - LastInterpolatedTime;

        public virtual void ProcessFrame()
        {
            if (SourceClock == null) return;

            clock.ProcessFrame();
            SourceClock.ProcessFrame();

            sourceIsRunning = SourceClock.IsRunning;

            LastInterpolatedTime = CurrentTime;

            if (!SourceClock.IsRunning)
                return;

            CurrentInterpolatedTime += clock.ElapsedFrameTime * Rate;

            if (Math.Abs(SourceClock.CurrentTime - CurrentInterpolatedTime) > AllowableErrorMilliseconds)
            {
                //if we've exceeded the allowable error, we should use the source clock's time value.
                CurrentInterpolatedTime = SourceClock.CurrentTime;
            }
            else
            {
                //if we differ from the elapsed time of the source, let's adjust for the difference.
                CurrentInterpolatedTime += (SourceClock.CurrentTime - CurrentInterpolatedTime) / 8;
            }
        }
    }
}
