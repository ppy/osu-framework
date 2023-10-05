// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A clock which uses an internal stopwatch to interpolate (smooth out) a source.
    /// </summary>
    public class InterpolatingFramedClock : IFrameBasedClock, ISourceChangeableClock // TODO: seal when DecoupleableInterpolatingFramedClock is gone.
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
        public double Drift => CurrentTime - FramedSourceClock.CurrentTime;

        public virtual double Rate => FramedSourceClock.Rate;

        public virtual bool IsRunning => sourceIsRunning;

        public virtual double ElapsedFrameTime { get; private set; }

        public IClock Source { get; private set; }

        protected IFrameBasedClock FramedSourceClock;

        public virtual double CurrentTime => currentTime;

        private readonly FramedClock realtimeClock = new FramedClock(new StopwatchClock(true));

        private double currentInterpolatedTime;

        private bool sourceIsRunning;

        private double currentTime;

        public InterpolatingFramedClock(IFrameBasedClock? source = null)
        {
            ChangeSource(source);
            Debug.Assert(Source != null);
            Debug.Assert(FramedSourceClock != null);
        }

        public virtual void ChangeSource(IClock? source)
        {
            if (source != null && source == Source)
                return;

            Source = source ?? new StopwatchClock(true);

            // We need a frame-based source to correctly process interpolation.
            // If the provided source is not already a framed clock, encapsulate it in one.
            FramedSourceClock = Source as IFrameBasedClock ?? new FramedClock(source);

            resetInterpolation();
        }

        public virtual void ProcessFrame()
        {
            double timeBefore = currentTime;

            realtimeClock.ProcessFrame();
            FramedSourceClock.ProcessFrame();

            sourceIsRunning = FramedSourceClock.IsRunning;

            bool sourceHasElapsed = FramedSourceClock.ElapsedFrameTime != 0;

            if (sourceIsRunning)
            {
                currentInterpolatedTime += realtimeClock.ElapsedFrameTime * Rate;

                if (!IsInterpolating || Math.Abs(FramedSourceClock.CurrentTime - currentInterpolatedTime) > AllowableErrorMilliseconds)
                {
                    // if we've exceeded the allowable error, we should use the source clock's time value.
                    // seeking backwards should only be allowed if the source is explicitly doing that.
                    currentInterpolatedTime = FramedSourceClock.ElapsedFrameTime < 0 ? FramedSourceClock.CurrentTime : Math.Max(timeBefore, FramedSourceClock.CurrentTime);

                    // once interpolation fails, we don't want to resume interpolating until the source clock starts to move again.
                    IsInterpolating = false;
                }
                else
                {
                    //if we differ from the elapsed time of the source, let's adjust for the difference.
                    currentInterpolatedTime += (FramedSourceClock.CurrentTime - currentInterpolatedTime) / 8;

                    // limit the direction of travel to avoid seeking against the flow.
                    currentInterpolatedTime = Rate >= 0 ? Math.Max(timeBefore, currentInterpolatedTime) : Math.Min(timeBefore, currentInterpolatedTime);
                }

                currentTime = currentInterpolatedTime;

                // Of importance, only start interpolating from the next frame.
                // The first frame after a clock starts may give very incorrect results, ie. due to a seek in the frame before.
                if (sourceHasElapsed)
                    IsInterpolating = true;
            }
            else
            {
                // If we detect a seek in the source while it's not running, immediately abort interpolation.
                if (sourceHasElapsed)
                    resetInterpolation();

                if (!IsInterpolating)
                    currentTime = FramedSourceClock.CurrentTime;
            }

            ElapsedFrameTime = currentTime - timeBefore;
        }

        private void resetInterpolation()
        {
            currentTime = 0;
            currentInterpolatedTime = 0;
            IsInterpolating = false;
        }

        double IFrameBasedClock.FramesPerSecond => 0;
    }
}
