// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Development;

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
    /// <remarks>
    /// There are a few things to note about this implementation:
    /// - Changing the source clock via <see cref="ChangeSource"/> will always take on the new source's running
    ///   state and current time, regardless of decoupled state.
    /// - It is always assumed that after a <see cref="Reset"/> on the source, it will be able to track time.
    /// - It is assumed that a source is generally able to start tracking from zero. Special handling ensures
    ///   that when arriving at zero from negative time, the source will attempt to be started once so it can
    ///   take over.
    ///   Note that no such special handling is assured for when the source has a maximum allowable time,
    ///   since it is not known what that time is.
    /// </remarks>
    public sealed class DecouplingFramedClock : ISourceChangeableClock, IAdjustableClock, IFrameBasedClock
    {
        /// <summary>
        /// Whether to allow operation in decoupled mode. Defaults to <c>true</c>.
        /// </summary>
        /// <remarks>
        /// When set to <c>false</c>, this clock will operate in a transparent pass-through mode.
        /// </remarks>
        public bool AllowDecoupling { get; set; } = true;

        public bool IsRunning { get; private set; }

        public double CurrentTime { get; private set; }

        public double ElapsedFrameTime { get; private set; }

        public double FramesPerSecond => 0;

        /// <summary>
        /// We maintain an internal running state so that when we notice the source clock has stopped,
        /// we can continue to run in a decoupled mode (and know if we should be running or not).
        /// </summary>
        private bool shouldBeRunning;

        /// <summary>
        /// We need to track our internal time separately from the exposed <see cref="CurrentTime"/> to make sure
        /// the exposed value is only ever updated on <see cref="ProcessFrame"/>.
        /// </summary>
        private double currentTime;

        /// <summary>
        /// Tracks the current time of <see cref="realtimeReferenceClock"/> one <see cref="ProcessFrame"/> ago.
        /// </summary>
        private double? lastReferenceTime;

        /// <summary>
        /// Whether the last <see cref="Seek"/> operation failed.
        /// This denotes that we need to <see cref="Start"/> in decoupled mode (if possible).
        /// </summary>
        private bool lastSeekFailed;

        /// <summary>
        /// This clock is used when we are decoupling from the source.
        /// </summary>
        private readonly IClock realtimeReferenceClock = DebugUtils.RealtimeClock ?? new StopwatchClock(true);

        private IAdjustableClock adjustableSourceClock;

        /// <summary>
        /// Denotes a state where a negative seek stopped the source clock and entered decoupled mode, meaning that
        /// after crossing into positive time again we should attempt to start and use the source clock.
        /// </summary>
        private bool pendingSourceRestartAfterNegativeSeek;

        public DecouplingFramedClock(IClock? source = null)
        {
            ChangeSource(source);
            Debug.Assert(Source != null);
            Debug.Assert(adjustableSourceClock != null);
        }

        public void ProcessFrame()
        {
            double lastTime = CurrentTime;

            (Source as IFrameBasedClock)?.ProcessFrame();

            try
            {
                // If the source is running, there is never a need for any decoupling logic.
                if (Source.IsRunning)
                {
                    currentTime = Source.CurrentTime;
                    shouldBeRunning = true;
                    return;
                }

                // If we're not allowed to decouple, we should also just pass-through the source time.
                if (!AllowDecoupling)
                {
                    currentTime = Source.CurrentTime;
                    shouldBeRunning = false;
                    return;
                }

                // We then want to check whether our internal running state permits time to elapse in decoupled mode.
                if (!shouldBeRunning)
                    return;

                // We can only begin tracking time from the second frame, as we need an elapsed real time reference.
                if (lastReferenceTime == null)
                    return;

                double elapsedReferenceTime = (realtimeReferenceClock.CurrentTime - lastReferenceTime.Value) * Rate;

                currentTime += elapsedReferenceTime;

                // When crossing into positive time, we should attempt to start and use the source clock.
                // Note that this carries the common assumption that the source clock *should* be able to run from zero.
                if (pendingSourceRestartAfterNegativeSeek && currentTime >= 0)
                {
                    pendingSourceRestartAfterNegativeSeek = false;

                    // We still need to check the seek was successful, else we might have already exceeded valid length of the source.
                    lastSeekFailed = !adjustableSourceClock.Seek(currentTime);
                    if (!lastSeekFailed)
                        adjustableSourceClock.Start();

                    // Don't use the source's time until next frame, as our decoupled time is likely more accurate
                    // (starting a clock, especially a TrackBass may have slight discrepancies).
                }
            }
            finally
            {
                IsRunning = shouldBeRunning;
                lastReferenceTime = realtimeReferenceClock.CurrentTime;
                CurrentTime = currentTime;
                ElapsedFrameTime = CurrentTime - lastTime;
            }
        }

        #region ISourceChangeableClock implementation

        public IClock Source { get; private set; }

        public void ChangeSource(IClock? source)
        {
            Source = source ?? new StopwatchClock(true);

            if (Source is not IAdjustableClock adjustableSource)
                throw new ArgumentException($"Clock must be of type {nameof(IAdjustableClock)}");

            adjustableSourceClock = adjustableSource;
            currentTime = adjustableSource.CurrentTime;
            shouldBeRunning = adjustableSource.IsRunning;
            lastSeekFailed = false;
        }

        #endregion

        #region IAdjustableClock implementation

        public void Reset()
        {
            adjustableSourceClock.Reset();
            pendingSourceRestartAfterNegativeSeek = false;
            shouldBeRunning = false;
            lastSeekFailed = false;
            currentTime = 0;
        }

        public void Start()
        {
            if (shouldBeRunning)
                return;

            // If the previous seek failed, avoid calling `Start` on the source clock.
            // Doing so would potentially cause it to start from an incorrect location (ie. 0 in the case where we are tracking negative time).
            if (lastSeekFailed && AllowDecoupling)
            {
                shouldBeRunning = true;
                return;
            }

            adjustableSourceClock.Start();
            shouldBeRunning = adjustableSourceClock.IsRunning || AllowDecoupling;
        }

        public void Stop()
        {
            adjustableSourceClock.Stop();
            shouldBeRunning = false;
        }

        public bool Seek(double position)
        {
            lastSeekFailed = !adjustableSourceClock.Seek(position);

            if (!lastSeekFailed)
            {
                // Transfer attempt to transfer decoupled running state to source
                // in the case we succeeded.
                if (shouldBeRunning && !Source.IsRunning)
                    adjustableSourceClock.Start();
            }
            else
            {
                if (!AllowDecoupling)
                    return false;

                // Ensure the underlying clock is stopped as we enter decoupled mode.
                adjustableSourceClock.Stop();
                pendingSourceRestartAfterNegativeSeek = position < 0;
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
