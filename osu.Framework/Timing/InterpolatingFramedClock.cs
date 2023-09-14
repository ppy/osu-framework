// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A clock which uses an internal stopwatch to interpolate (smooth out) a source.
    /// </summary>
    public class InterpolatingFramedClock : IFrameBasedClock, ISourceChangeableClock
    {
        /// <summary>
        /// The amount of error that is allowed between the source and interpolated time before the interpolated time is ignored and the source time is used.
        /// </summary>
        public virtual double AllowableErrorMilliseconds => 1000.0 / 60 * 2 * Rate;

        /// <summary>
        /// Whether interpolation was applied at the last processed frame.
        /// </summary>
        /// <remarks>
        /// If <see cref="Drift"/> becomes too high (as defined by <see cref="AllowableErrorMilliseconds"/>),
        /// interpolation will be bypassed in order to provide a more correct time value.
        /// </remarks>
        public bool IsInterpolating { get; private set; }

        /// <summary>
        /// The drift in milliseconds between the source and interpolation at the last processed frame.
        /// </summary>
        public double Drift => CurrentTime - (FramedSourceClock?.CurrentTime ?? 0);

        public virtual double Rate
        {
            get => FramedSourceClock?.Rate ?? 1;
            set => throw new NotSupportedException();
        }

        public virtual bool IsRunning => sourceIsRunning;

        public virtual double ElapsedFrameTime => currentInterpolatedTime - lastInterpolatedTime;

        public IClock? Source { get; private set; }

        public virtual double CurrentTime => currentTime;

        protected IFrameBasedClock? FramedSourceClock;

        private readonly FramedClock realtimeClock = new FramedClock(new StopwatchClock(true));

        private double lastInterpolatedTime;

        private double currentInterpolatedTime;

        private bool sourceIsRunning;

        private double currentTime;

        public InterpolatingFramedClock(IClock? source = null)
        {
            ChangeSource(source);
        }

        public virtual void ChangeSource(IClock? source)
        {
            Source = source ?? new StopwatchClock(true);

            // We need a frame-based source to correctly process interpolation.
            // If the provided source is not already a framed clock, encapsulate it in one.
            FramedSourceClock = Source as IFrameBasedClock ?? new FramedClock(source);

            resetInterpolation();
        }

        public virtual void ProcessFrame()
        {
            if (FramedSourceClock == null) return;

            realtimeClock.ProcessFrame();
            FramedSourceClock.ProcessFrame();

            sourceIsRunning = FramedSourceClock.IsRunning;

            lastInterpolatedTime = currentTime;

            if (FramedSourceClock.IsRunning)
            {
                if (FramedSourceClock.ElapsedFrameTime != 0)
                    IsInterpolating = true;

                currentInterpolatedTime += realtimeClock.ElapsedFrameTime * Rate;

                if (!IsInterpolating || Math.Abs(FramedSourceClock.CurrentTime - currentInterpolatedTime) > AllowableErrorMilliseconds)
                {
                    // if we've exceeded the allowable error, we should use the source clock's time value.
                    // seeking backwards should only be allowed if the source is explicitly doing that.
                    currentInterpolatedTime = FramedSourceClock.ElapsedFrameTime < 0 ? FramedSourceClock.CurrentTime : Math.Max(lastInterpolatedTime, FramedSourceClock.CurrentTime);

                    // once interpolation fails, we don't want to resume interpolating until the source clock starts to move again.
                    IsInterpolating = false;
                }
                else
                {
                    //if we differ from the elapsed time of the source, let's adjust for the difference.
                    currentInterpolatedTime += (FramedSourceClock.CurrentTime - currentInterpolatedTime) / 8;

                    // limit the direction of travel to avoid seeking against the flow.
                    currentInterpolatedTime = Rate >= 0 ? Math.Max(lastInterpolatedTime, currentInterpolatedTime) : Math.Min(lastInterpolatedTime, currentInterpolatedTime);
                }
            }

            currentTime = sourceIsRunning ? currentInterpolatedTime : FramedSourceClock.CurrentTime;
        }

        private void resetInterpolation()
        {
            currentTime = 0;
            lastInterpolatedTime = 0;
            currentInterpolatedTime = 0;
        }

        double IFrameBasedClock.FramesPerSecond => throw new NotImplementedException();
    }
}
