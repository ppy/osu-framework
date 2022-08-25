// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

namespace osu.Framework.Timing
{
    /// <summary>
    /// Adds the ability to keep the clock running even when the underlying source has stopped or cannot handle the current time range.
    /// This is handled by performing seeks on the underlying source and checking whether they were successful or not.
    /// On failure to seek, we take over with an internal clock until control can be returned to the actual source.
    ///
    /// This clock type removes the requirement of having a source set.
    ///
    /// If a <see cref="InterpolatingFramedClock.Source"/> is set, it is presumed that we have exclusive control over operations on it.
    /// This is used to our advantage to allow correct <see cref="IsRunning"/> state tracking in the event of cross-thread communication delays (with an audio thread, for instance).
    /// </summary>
    public class DecoupleableInterpolatingFramedClock : InterpolatingFramedClock, IAdjustableClock
    {
        /// <summary>
        /// Specify whether we are coupled 1:1 to SourceClock. If not, we can independently continue operation.
        /// </summary>
        public bool IsCoupled = true;

        /// <summary>
        /// In some cases we should always use the interpolated source.
        /// </summary>
        private bool useInterpolatedSourceTime => IsRunning && FramedSourceClock?.IsRunning == true;

        private readonly FramedClock decoupledClock;
        private readonly StopwatchClock decoupledStopwatch;

        /// <summary>
        /// We need to be able to pass on adjustments to the source if it supports them.
        /// </summary>
        private IAdjustableClock? adjustableSource => Source as IAdjustableClock;

        public override double CurrentTime => currentTime;

        private double currentTime;

        public double ProposedCurrentTime => useInterpolatedSourceTime ? base.CurrentTime : decoupledClock.CurrentTime;

        public double ProposedElapsedTime => useInterpolatedSourceTime ? base.ElapsedFrameTime : decoupledClock.ElapsedFrameTime;

        public override bool IsRunning => decoupledClock.IsRunning; // we always want to use our local IsRunning state, as it is more correct.

        private double elapsedFrameTime;

        public override double ElapsedFrameTime => elapsedFrameTime;

        public override double Rate
        {
            get => Source?.Rate ?? 1;
            set
            {
                if (adjustableSource == null)
                    throw new NotSupportedException("Source is not adjustable.");

                adjustableSource.Rate = value;
            }
        }

        public void ResetSpeedAdjustments() => Rate = 1;

        public DecoupleableInterpolatingFramedClock()
        {
            decoupledClock = new FramedClock(decoupledStopwatch = new StopwatchClock());
        }

        public override void ProcessFrame()
        {
            base.ProcessFrame();

            bool sourceRunning = Source?.IsRunning ?? false;

            decoupledStopwatch.Rate = adjustableSource?.Rate ?? 1;

            // if interpolating based on the source, keep the decoupled clock in sync with the interpolated time.
            if (IsCoupled && sourceRunning)
                decoupledStopwatch.Seek(base.CurrentTime);

            // process the decoupled clock to update the current proposed time.
            decoupledClock.ProcessFrame();

            if (IsRunning)
            {
                if (IsCoupled)
                {
                    // when coupled, we want to stop when our source clock stops.
                    if (!sourceRunning)
                    {
                        Stop();

                        // if the source stops, ensure that we are immediately in sync with its time value.
                        //
                        // note that this *won't* apply when a Stop() call is made. in such a case, the interpolated value will
                        // remain as current (as this is more expected behaviour – if we did a transfer there would be a jump, potentially
                        // backwards.
                        if (adjustableSource != null)
                        {
                            decoupledStopwatch.Seek(adjustableSource.CurrentTime);
                            decoupledClock.ProcessFrame();
                        }
                    }
                }
                else
                {
                    // when decoupled and running, we should try to start the source clock it if it's capable of handling the current time.
                    if (!sourceRunning)
                        Start();
                }
            }
            else if (IsCoupled && sourceRunning)
            {
                // when coupled and not running, we want to start when the source clock starts.
                Start();
            }

            // if the source clock is started as a result of becoming capable of handling the decoupled time, the proposed time may change to reflect the interpolated source time.
            // however the interpolated source time that was calculated inside base.ProcessFrame() (above) did not consider the current (post-seek) time of the source.
            // in all other cases the proposed time will match before and after clocks are started/stopped.
            double proposedTime = ProposedCurrentTime;
            double elapsedTime = ProposedElapsedTime;

            elapsedFrameTime = elapsedTime;

            // the source may be started during playback but remain behind the current time in the playback direction for a number of frames.
            // in such cases, the current time should remain paused until the source time catches up.
            currentTime = elapsedFrameTime < 0 ? Math.Min(currentTime, proposedTime) : Math.Max(currentTime, proposedTime);
        }

        public override void ChangeSource(IClock? source)
        {
            if (source == null) return;

            // transfer our value to the source clock.
            (source as IAdjustableClock)?.Seek(CurrentTime);

            base.ChangeSource(source);
        }

        public void Reset()
        {
            IsCoupled = true;

            adjustableSource?.Reset();
            decoupledStopwatch.Reset();
        }

        public void Start()
        {
            if (adjustableSource?.IsRunning == false)
            {
                if (adjustableSource.Seek(ProposedCurrentTime))
                    //only start the source clock if our time values match.
                    //this handles the case where we seeked to an unsupported value and the source clock is out of sync.
                    adjustableSource.Start();
            }

            decoupledStopwatch.Start();
        }

        public void Stop()
        {
            decoupledStopwatch.Stop();
            adjustableSource?.Stop();
        }

        public bool Seek(double position)
        {
            try
            {
                // To simplify seek processing, handle the case of a null source up-front.
                if (adjustableSource == null)
                {
                    decoupledStopwatch.Seek(position);
                    return true;
                }

                Debug.Assert(adjustableSource != null);

                if (IsCoupled)
                {
                    // Begin by performing a seek on the source clock.
                    bool success = adjustableSource.Seek(position);

                    // If coupled, regardless of the success of the seek on the source, use the updated
                    // source's current position. This is done because in the case of a seek failure, the
                    // source may update the value
                    decoupledStopwatch.Seek(adjustableSource.CurrentTime);

                    return success;
                }
                else
                {
                    // If decoupled, a seek operation should cause the decoupled clock to seek regardless
                    // of whether the source clock could handle the target location.

                    // In the case the source is running, attempt a seek and stop it if that seek fails.
                    // Note that we don't need to perform a seek if the source is not running.
                    // This is important to improve performance in the decoupled case if the source clock's Seek call is not immediate.
                    if (adjustableSource.IsRunning && !adjustableSource.Seek(position))
                        adjustableSource?.Stop();

                    // ..then perform the requested seek precisely on the decoupled clock.
                    return decoupledStopwatch.Seek(position);
                }
            }
            finally
            {
                ProcessFrame();
            }
        }
    }
}
