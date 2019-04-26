// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

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
        private IAdjustableClock adjustableSource => Source as IAdjustableClock;

        public override double CurrentTime => currentTime;

        private double currentTime;

        public override bool IsRunning => decoupledClock.IsRunning; // we always want to use our local IsRunning state, as it is more correct.

        private double elapsedFrameTime;

        public override double ElapsedFrameTime => elapsedFrameTime;

        public override double Rate
        {
            get => Source?.Rate ?? 1;
            set => adjustableSource.Rate = value;
        }

        public void ResetSpeedAdjustments() => Rate = 1;

        public DecoupleableInterpolatingFramedClock()
        {
            decoupledClock = new FramedClock(decoupledStopwatch = new StopwatchClock());
        }

        public override void ProcessFrame()
        {
            base.ProcessFrame();

            decoupledStopwatch.Rate = adjustableSource?.Rate ?? 1;

            bool sourceRunning = Source?.IsRunning ?? false;

            if (IsRunning)
            {
                if (IsCoupled)
                {
                    // when coupled, we want to stop when our source clock stops.
                    if (sourceRunning)
                        decoupledStopwatch.Seek(base.CurrentTime);
                    else
                        Stop();
                }
                else
                {
                    // when decoupled, if we're running but our source isn't, we should try a seek to see if it's capable to handle the current time.
                    if (!sourceRunning)
                        Start();
                }
            }
            else if (IsCoupled && sourceRunning)
            {
                Start();
                decoupledStopwatch.Seek(CurrentTime);
            }

            decoupledClock.ProcessFrame();

            double proposedTime = useInterpolatedSourceTime ? base.CurrentTime : decoupledClock.CurrentTime;

            elapsedFrameTime = useInterpolatedSourceTime ? base.ElapsedFrameTime : decoupledClock.ElapsedFrameTime;

            currentTime = elapsedFrameTime < 0 ? proposedTime : Math.Max(currentTime, proposedTime);
        }

        public override void ChangeSource(IClock source)
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
                if (adjustableSource.Seek(CurrentTime))
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
                bool success = adjustableSource?.Seek(position) != false;

                if (IsCoupled)
                    return success;

                if (!success)
                    //if we failed to seek then stop the source and use decoupled mode.
                    adjustableSource?.Stop();

                return decoupledStopwatch.Seek(position);
            }
            finally
            {
                ProcessFrame();
            }
        }
    }
}
