// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using SDL;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A FrameClock which will limit the number of frames processed by adding Thread.Sleep calls on each ProcessFrame.
    /// </summary>
    public class ThrottledFrameClock : FramedClock
    {
        /// <summary>
        /// The target number of updates per second. Only used when <see cref="Throttling"/> is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// A value of 0 is treated the same as "unlimited" or <see cref="double.MaxValue"/>.
        /// </remarks>
        public double MaximumUpdateHz = 1000.0;

        /// <summary>
        /// Whether throttling should be enabled. Defaults to <c>true</c>.
        /// </summary>
        public bool Throttling = true;

        /// <summary>
        /// The time spent in a Thread.Sleep state during the last frame.
        /// </summary>
        public double TimeSlept { get; private set; }

        public override void ProcessFrame()
        {
            Debug.Assert(MaximumUpdateHz >= 0);

            base.ProcessFrame();

            if (Throttling)
            {
                if (MaximumUpdateHz > 0 && MaximumUpdateHz < double.MaxValue)
                {
                    throttle();
                }
                else
                {
                    // Even when running at unlimited frame-rate, we should call the scheduler
                    // to give lower-priority background processes a chance to do work.
                    TimeSlept = sleepAndUpdateCurrent(0);
                }
            }
            else
            {
                TimeSlept = 0;
            }

            Debug.Assert(TimeSlept <= ElapsedFrameTime);
        }

        private double accumulatedSleepError;

        private void throttle()
        {
            double excessFrameTime = 1000d / MaximumUpdateHz - ElapsedFrameTime;

            TimeSlept = sleepAndUpdateCurrent(Math.Max(0, excessFrameTime + accumulatedSleepError));

            accumulatedSleepError += excessFrameTime - TimeSlept;

            // Never allow the sleep error to become too negative and induce too many catch-up frames
            accumulatedSleepError = Math.Max(-1000 / 30.0, accumulatedSleepError);
        }

        private double sleepAndUpdateCurrent(double milliseconds)
        {
            // By returning here, in cases where the game is not keeping up, we don't yield.
            // Not 100% sure if we want to do this, but let's give it a try.
            if (milliseconds <= 0)
                return 0;

            double before = CurrentTime;

            SDL3.SDL_DelayNS((ulong)(milliseconds * SDL3.SDL_NS_PER_MS));

            return (CurrentTime = SourceTime) - before;
        }
    }
}
