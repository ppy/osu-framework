// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A decoupling clock allows taking an existing clock which has seek limitations and provides
    /// a controllable encapsulated view of that clock with the ability to seek and track time
    /// outside of the normally seekable bounds.
    ///
    /// Put simply, it will take a Track which can only track time from 0..trackLength and allow
    /// both negative seeks and seeks beyond trackLength. It will also allow time to continue counting
    /// beyond the end of the track even when not explicitly seeked.
    /// </summary>
    public sealed class DecouplingClock : ISourceChangeableClock, IAdjustableClock
    {
        public bool AllowDecoupling = true;

        public IClock Source { get; private set; }

        /// <summary>
        /// This clock is used when we are decoupling from the source.
        /// </summary>
        private readonly StopwatchClock realtimeReferenceClock = new StopwatchClock(true);

        private IAdjustableClock adjustableSourceClock;

        private bool isRunning;
        private double? lastReferenceTimeConsumed;

        /// <summary>
        /// Whether the last <see cref="Seek"/> operation failed.
        /// This denotes that we need to <see cref="Start"/> in decoupled mode (if possible).
        /// </summary>
        private bool lastSeekFailed;

        public bool IsRunning
        {
            get
            {
                // Always use the source clock's running state if it's running.
                if (Source.IsRunning)
                    return isRunning = true;

                // If we're allowed to be decoupled, maintain our internal running state.
                if (AllowDecoupling)
                    return isRunning;

                // Otherwise we're definitely not running.
                Debug.Assert(!Source.IsRunning);
                return isRunning = false;
            }
        }

        private double currentTime;

        public double CurrentTime
        {
            get
            {
                try
                {
                    if (Source.IsRunning || !AllowDecoupling)
                        return currentTime = Source.CurrentTime;

                    if (!isRunning)
                        return currentTime;

                    if (lastReferenceTimeConsumed == null)
                        return currentTime;

                    double elapsedSinceLastCall = (realtimeReferenceClock.CurrentTime - lastReferenceTimeConsumed.Value) * Rate;

                    // Crossing the zero time boundary, we can attempt to start and use the source clock.
                    // TODO: consider if this is too specific. may need to be mentioned in class xmldoc if we keep it.
                    if (currentTime < 0 && currentTime + elapsedSinceLastCall >= 0)
                    {
                        adjustableSourceClock.Start();
                        if (Source.IsRunning)
                            return currentTime = Source.CurrentTime;
                    }

                    return currentTime += elapsedSinceLastCall;
                }
                finally
                {
                    lastReferenceTimeConsumed = realtimeReferenceClock.CurrentTime;
                }
            }
        }

        public DecouplingClock(IClock? source = null)
        {
            ChangeSource(source);
            Debug.Assert(Source != null);
            Debug.Assert(adjustableSourceClock != null);
        }

        public void ChangeSource(IClock? source)
        {
            Source = source ?? new StopwatchClock(true);

            if (Source is not IAdjustableClock adjustableSource)
                throw new ArgumentException($"Clock must be of type {nameof(IAdjustableClock)}");

            adjustableSourceClock = adjustableSource;
            isRunning = adjustableSource.IsRunning;
        }

        #region IAdjustableClock implementation

        public void Reset()
        {
            adjustableSourceClock.Reset();
            isRunning = false;
            lastSeekFailed = false;
        }

        public void Start()
        {
            if (isRunning)
                return;

            // If the previous seek failed, avoid calling `Start` on the source clock.
            // Doing so would potentially cause it to start from an incorrect location (ie. 0 in the case where we are tracking negative time).
            // TODO: add test coverage
            if (lastSeekFailed && AllowDecoupling)
            {
                isRunning = true;
                return;
            }

            adjustableSourceClock.Start();
            isRunning = adjustableSourceClock.IsRunning || AllowDecoupling;
        }

        public void Stop()
        {
            adjustableSourceClock.Stop();
            isRunning = false;
        }

        public bool Seek(double position)
        {
            lastSeekFailed = !adjustableSourceClock.Seek(position);

            if (!lastSeekFailed)
            {
                // Transfer attempt to transfer decoupled running state to source
                // in the case we succeeded.
                if (isRunning && !Source.IsRunning)
                    adjustableSourceClock.Start();
            }
            else
            {
                if (!AllowDecoupling)
                    return false;

                // Ensure the underlying clock is stopped as we enter decoupled mode.
                // TODO: test coverage
                adjustableSourceClock.Stop();
            }

            currentTime = position;
            return true;
        }

        public void ResetSpeedAdjustments() => adjustableSourceClock.ResetSpeedAdjustments();

        public double Rate
        {
            get => adjustableSourceClock.Rate;
            set => adjustableSourceClock.Rate = value;
        }

        #endregion
    }
}
