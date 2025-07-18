// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A clock which uses an internal stopwatch to interpolate (smooth out) a source.
    /// </summary>
    public sealed class InterpolatingFramedClock : IFrameBasedClock, ISourceChangeableClock
    {
        /// <summary>
        /// The amount of error that is allowed between the source and interpolated time before the interpolated time is ignored and the source time is used.
        /// Defaults to two 60 fps frames (~33.3 ms).
        /// </summary>
        /// <remarks>
        /// This is internally adjusted for the current playback rate (so that the actual precision is constant regardless of the rate applied).
        /// </remarks>
        public double AllowableErrorMilliseconds { get; set; } = 1000.0 / 60 * 2;

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
        public double Drift => CurrentTime - framedSourceClock.CurrentTime;

        public double Rate => framedSourceClock.Rate;

        public bool IsRunning { get; private set; }

        public double ElapsedFrameTime { get; private set; }

        public IClock Source { get; private set; }

        private IFrameBasedClock framedSourceClock;

        public double CurrentTime { get; private set; }

        private readonly FramedClock realtimeClock = new FramedClock(new StopwatchClock(true));

        private double currentTime;

        public InterpolatingFramedClock(IFrameBasedClock? source = null)
        {
            ChangeSource(source);
            Debug.Assert(Source != null);
            Debug.Assert(framedSourceClock != null);
        }

        public void ChangeSource(IClock? source)
        {
            if (source != null && source == Source)
                return;

            Source = source ?? new StopwatchClock(true);

            // We need a frame-based source to correctly process interpolation.
            // If the provided source is not already a framed clock, encapsulate it in one.
            framedSourceClock = Source as IFrameBasedClock ?? new FramedClock(source);

            IsInterpolating = false;
            currentTime = framedSourceClock.CurrentTime;
        }

        public void ProcessFrame()
        {
            double lastTime = currentTime;

            realtimeClock.ProcessFrame();
            framedSourceClock.ProcessFrame();

            bool sourceIsRunning = framedSourceClock.IsRunning;

            bool sourceHasElapsed = framedSourceClock.ElapsedFrameTime != 0;

            try
            {
                if (!sourceIsRunning)
                {
                    // While the source isn't running, we remain in the current interpolation mode unless there's a seek.
                    // This is to ensure the most consistent playback possible, and avoid fractional differences when stopping/starting the source.
                    if (sourceHasElapsed)
                    {
                        IsInterpolating = false;
                        currentTime = framedSourceClock.CurrentTime;
                    }

                    return;
                }

                if (IsInterpolating)
                {
                    // apply time increase from interpolation.
                    currentTime += realtimeClock.ElapsedFrameTime * Rate;
                    // if we differ from the elapsed time of the source, let's adjust for the difference.
                    // TODO: this is frame rate depending, and can result in unexpected results.
                    currentTime += (framedSourceClock.CurrentTime - currentTime) / 8;

                    bool withinAllowableError = Math.Abs(framedSourceClock.CurrentTime - currentTime) <= AllowableErrorMilliseconds * Rate;

                    if (!withinAllowableError)
                    {
                        // if we've exceeded the allowable error, we should use the source clock's time value.
                        IsInterpolating = false;
                        currentTime = framedSourceClock.CurrentTime;
                    }
                }
                else
                {
                    currentTime = framedSourceClock.CurrentTime;

                    // Of importance, only start interpolating from the next frame.
                    // The first frame after a clock starts may give very incorrect results, ie. due to a seek in the frame before.
                    if (sourceHasElapsed)
                        IsInterpolating = true;
                }

                // seeking backwards should only be allowed if the source is explicitly doing that.
                bool elapsedInOpposingDirection = framedSourceClock.ElapsedFrameTime != 0 && Math.Sign(framedSourceClock.ElapsedFrameTime) != Math.Sign(Rate);
                if (!elapsedInOpposingDirection)
                    currentTime = Rate >= 0 ? Math.Max(lastTime, currentTime) : Math.Min(lastTime, currentTime);
            }
            finally
            {
                IsRunning = sourceIsRunning;
                CurrentTime = currentTime;
                ElapsedFrameTime = currentTime - lastTime;
            }
        }

        double IFrameBasedClock.FramesPerSecond => 0;
    }
}
