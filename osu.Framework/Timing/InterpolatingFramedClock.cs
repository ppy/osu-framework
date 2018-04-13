// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
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

        protected IClock SourceClock;

        protected IFrameBasedClock FramedSourceClock;
        protected double LastInterpolatedTime;
        protected double CurrentInterpolatedTime;

        public FrameTimeInfo TimeInfo => new FrameTimeInfo { Elapsed = ElapsedFrameTime, Current = CurrentTime };

        public double AverageFrameTime { get; } = 0;

        public double FramesPerSecond { get; } = 0;

        public virtual void ChangeSource(IClock source)
        {
            if (source != null)
            {
                SourceClock = source;
                FramedSourceClock = SourceClock as IFrameBasedClock ?? new FramedClock(SourceClock);
            }

            LastInterpolatedTime = 0;
            CurrentInterpolatedTime = 0;
        }

        public InterpolatingFramedClock(IClock source = null)
        {
            ChangeSource(source);
        }

        public virtual double CurrentTime => sourceIsRunning ? CurrentInterpolatedTime : FramedSourceClock.CurrentTime;

        public double AllowableErrorMilliseconds = 1000.0 / 60 * 2;

        private bool sourceIsRunning;

        public virtual double Rate
        {
            get { return FramedSourceClock.Rate; }
            set { throw new NotSupportedException(); }
        }

        public virtual bool IsRunning => sourceIsRunning;

        public virtual double Drift => CurrentTime - FramedSourceClock.CurrentTime;

        public virtual double ElapsedFrameTime => CurrentInterpolatedTime - LastInterpolatedTime;

        public virtual void ProcessFrame()
        {
            if (FramedSourceClock == null) return;

            clock.ProcessFrame();
            FramedSourceClock.ProcessFrame();

            sourceIsRunning = FramedSourceClock.IsRunning;

            LastInterpolatedTime = CurrentTime;

            if (!FramedSourceClock.IsRunning)
                return;

            CurrentInterpolatedTime += clock.ElapsedFrameTime * Rate;

            if (Math.Abs(FramedSourceClock.CurrentTime - CurrentInterpolatedTime) > AllowableErrorMilliseconds)
            {
                //if we've exceeded the allowable error, we should use the source clock's time value.
                CurrentInterpolatedTime = FramedSourceClock.CurrentTime;
            }
            else
            {
                //if we differ from the elapsed time of the source, let's adjust for the difference.
                CurrentInterpolatedTime += (FramedSourceClock.CurrentTime - CurrentInterpolatedTime) / 8;
            }
        }
    }
}
