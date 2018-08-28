// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Threading;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A FrameClock which will limit the number of frames processed by adding Thread.Sleep calls on each ProcessFrame.
    /// </summary>
    public class ThrottledFrameClock : FramedClock
    {
        /// <summary>
        /// The number of updated per second which is permitted.
        /// </summary>
        public double MaximumUpdateHz = 1000.0;

        /// <summary>
        /// The time spent in a Thread.Sleep state during the last frame.
        /// </summary>
        public double SleptTime { get; private set; }

        private double accumulatedSleepError;

        private void throttle()
        {
            bool shouldYield = true;

            //If we are limiting to a specific rate, and not enough time has passed for the next frame to be accepted we should pause here.
            if (MaximumUpdateHz > 0)
            {
                double targetMilliseconds = MaximumUpdateHz > 0 ? 1000.0 / MaximumUpdateHz : 0;

                if (ElapsedFrameTime < targetMilliseconds)
                {
                    double excessFrameTime = targetMilliseconds - ElapsedFrameTime;

                    int timeToSleepFloored = (int)Math.Floor(excessFrameTime);

                    Trace.Assert(timeToSleepFloored >= 0);

                    accumulatedSleepError += excessFrameTime - timeToSleepFloored;
                    int accumulatedSleepErrorCompensation = (int)Math.Round(accumulatedSleepError);

                    // Can't sleep a negative amount of time
                    accumulatedSleepErrorCompensation = Math.Max(accumulatedSleepErrorCompensation, -timeToSleepFloored);

                    accumulatedSleepError -= accumulatedSleepErrorCompensation;
                    timeToSleepFloored += accumulatedSleepErrorCompensation;

                    // We don't want re-schedules with Thread.Sleep(0). We already have that case down below.
                    if (timeToSleepFloored > 0)
                    {
                        Thread.Sleep(timeToSleepFloored);
                        shouldYield = false;
                    }

                    // Sleep is not guaranteed to be an exact time. It only guaranteed to sleep AT LEAST the specified time. We also used some time to compute the above things, so this is also factored in here.
                    double afterSleepTime = SourceTime;
                    SleptTime = afterSleepTime - CurrentTime;
                    accumulatedSleepError += timeToSleepFloored - (afterSleepTime - CurrentTime);
                    CurrentTime = afterSleepTime;
                }
                else
                {
                    // We use the negative spareTime to compensate for framerate jitter slightly.
                    double spareTime = ElapsedFrameTime - targetMilliseconds;
                    SleptTime = 0;
                    accumulatedSleepError = -spareTime;
                }
            }

            // Call the scheduler to give lower-priority background processes a chance to do stuff.
            if (shouldYield)
                Thread.Sleep(0);
        }

        public override void ProcessFrame()
        {
            base.ProcessFrame();
            throttle();
        }
    }
}
