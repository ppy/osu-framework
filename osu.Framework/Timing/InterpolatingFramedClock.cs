// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A clock which uses an internal stopwatch to interpolate (smooth out) a source.
    /// Note that this will NOT function unless a source has been set.
    /// </summary>
    public class InterpolatingFramedClock : IFrameBasedClock, ISourceChangeableClock
    {
        private readonly FramedClock clock = new FramedClock(new StopwatchClock(true));

        public IClock? Source { get; private set; }

        protected IFrameBasedClock? FramedSourceClock;
        protected double LastInterpolatedTime;
        protected double CurrentInterpolatedTime;

        public FrameTimeInfo TimeInfo => new FrameTimeInfo { Elapsed = ElapsedFrameTime, Current = CurrentTime };

        public double FramesPerSecond => 0;

        public virtual void ChangeSource(IClock? source)
        {
            if (source != null)
            {
                Source = source;
                FramedSourceClock = Source as IFrameBasedClock ?? new FramedClock(Source);
            }

            LastInterpolatedTime = 0;
            CurrentInterpolatedTime = 0;
        }

        public InterpolatingFramedClock(IClock? source = null)
        {
            ChangeSource(source);
        }

        public virtual double CurrentTime => currentTime;

        private double currentTime;

        /// <summary>
        /// The amount of error that is allowed between the source and interpolated time before the interpolated time is ignored and the source time is used.
        /// </summary>
        public virtual double AllowableErrorMilliseconds => 1000.0 / 60 * 2 * Rate;

        private bool sourceIsRunning;

        public virtual double Rate
        {
            get => FramedSourceClock?.Rate ?? 1;
            set => throw new NotSupportedException();
        }

        public virtual bool IsRunning => sourceIsRunning;

        public virtual double Drift => CurrentTime - (FramedSourceClock?.CurrentTime ?? 0);

        public virtual double ElapsedFrameTime => CurrentInterpolatedTime - LastInterpolatedTime;

        /// <summary>
        /// Whether time is being interpolated for the frame currently being processed.
        /// </summary>
        public bool IsInterpolating { get; private set; }

        public virtual void ProcessFrame()
        {
            if (FramedSourceClock == null) return;

            clock.ProcessFrame();
            FramedSourceClock.ProcessFrame();

            sourceIsRunning = FramedSourceClock.IsRunning;

            LastInterpolatedTime = currentTime;

            if (FramedSourceClock.IsRunning)
            {
                if (FramedSourceClock.ElapsedFrameTime != 0)
                    IsInterpolating = true;

                CurrentInterpolatedTime += clock.ElapsedFrameTime * Rate;

                if (!IsInterpolating || Math.Abs(FramedSourceClock.CurrentTime - CurrentInterpolatedTime) > AllowableErrorMilliseconds)
                {
                    // if we've exceeded the allowable error, we should use the source clock's time value.
                    // seeking backwards should only be allowed if the source is explicitly doing that.
                    CurrentInterpolatedTime = FramedSourceClock.ElapsedFrameTime < 0 ? FramedSourceClock.CurrentTime : Math.Max(LastInterpolatedTime, FramedSourceClock.CurrentTime);

                    // once interpolation fails, we don't want to resume interpolating until the source clock starts to move again.
                    IsInterpolating = false;
                }
                else
                {
                    //if we differ from the elapsed time of the source, let's adjust for the difference.
                    CurrentInterpolatedTime += (FramedSourceClock.CurrentTime - CurrentInterpolatedTime) / 8;

                    // limit the direction of travel to avoid seeking against the flow.
                    CurrentInterpolatedTime = Rate >= 0 ? Math.Max(LastInterpolatedTime, CurrentInterpolatedTime) : Math.Min(LastInterpolatedTime, CurrentInterpolatedTime);
                }
            }

            currentTime = sourceIsRunning ? CurrentInterpolatedTime : FramedSourceClock.CurrentTime;
        }
    }
}
