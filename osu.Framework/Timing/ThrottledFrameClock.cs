// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using osu.Framework.Platform.Windows.Native;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A FrameClock which will limit the number of frames processed by adding Thread.Sleep calls on each ProcessFrame.
    /// </summary>
    public class ThrottledFrameClock : FramedClock, IDisposable
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

        private IntPtr waitableTimer;

        internal ThrottledFrameClock()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows) createWaitableTimer();
        }

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

            TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);

            if (!waitWaitableTimer(timeSpan))
                Thread.Sleep(timeSpan);

            return (CurrentTime = SourceTime) - before;
        }

        public void Dispose()
        {
            if (waitableTimer != IntPtr.Zero)
                Execution.CloseHandle(waitableTimer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool waitWaitableTimer(TimeSpan timeSpan)
        {
            if (waitableTimer == IntPtr.Zero) return false;

            // Not sure if we want to fall back to Thread.Sleep on failure here, needs further investigation.
            if (Execution.SetWaitableTimerEx(waitableTimer, Execution.CreateFileTime(timeSpan), 0, null, default, IntPtr.Zero, 0))
            {
                Execution.WaitForSingleObject(waitableTimer, Execution.INFINITE);
                return true;
            }

            return false;
        }

        private void createWaitableTimer()
        {
            try
            {
                // Attempt to use CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, only available since Windows 10, version 1803.
                waitableTimer = Execution.CreateWaitableTimerEx(IntPtr.Zero, null,
                    Execution.CreateWaitableTimerFlags.CREATE_WAITABLE_TIMER_MANUAL_RESET | Execution.CreateWaitableTimerFlags.CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, Execution.TIMER_ALL_ACCESS);

                if (waitableTimer == IntPtr.Zero)
                {
                    // Fall back to a more supported version. This is still far more accurate than Thread.Sleep.
                    waitableTimer = Execution.CreateWaitableTimerEx(IntPtr.Zero, null, Execution.CreateWaitableTimerFlags.CREATE_WAITABLE_TIMER_MANUAL_RESET, Execution.TIMER_ALL_ACCESS);
                }
            }
            catch
            {
                // Any kind of unexpected exception should fall back to Thread.Sleep.
            }
        }
    }
}
