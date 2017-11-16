// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Logging;
using osu.Framework.Timing;
using System;
using System.Diagnostics;
using System.Threading;

namespace osu.Framework.Statistics
{
    internal class BackgroundStackTraceCollector
    {
        private StackTrace backgroundMonitorStackTrace;

        private readonly StopwatchClock clock;

        private readonly Logger logger;
        private readonly Thread targetThread;

        internal double LastConsumptionTime;

        internal double SpikeRecordDuration;

        public BackgroundStackTraceCollector(Thread targetThread, StopwatchClock clock)
        {
            if (Debugger.IsAttached) return;

            logger = Logger.GetLogger(LoggingTarget.Performance);

            this.clock = clock;
            this.targetThread = targetThread;

            // we can't run under mono.
            if (Type.GetType("Mono.Runtime") != null) return;

            var backgroundMonitorThread = new Thread(() =>
            {
                while (true)
                {
                    if (targetThread.IsAlive && clock.ElapsedMilliseconds - LastConsumptionTime > SpikeRecordDuration && backgroundMonitorStackTrace == null)
                        backgroundMonitorStackTrace = safelyGetStackTrace(targetThread);

                    Thread.Sleep(1);
                }

                // ReSharper disable once FunctionNeverReturns
            }) { IsBackground = true };

            backgroundMonitorThread.Start();
        }

        internal void LogFrame(double elapsedFrameTime)
        {
            StackTrace trace = backgroundMonitorStackTrace;
            backgroundMonitorStackTrace = null;

            logger.Add();
            logger.Add($@"---------- Slow Frame Detected on {targetThread.Name} at {clock.CurrentTime / 1000:#0.00}s ----------");

            logger.Add();

            var frames = trace?.GetFrames();

            if (frames != null)
            {
                logger.Add(@"Call stack follows:");
                logger.Add();

                foreach (StackFrame f in frames)
                    logger.Add($@"- {f.GetMethod()} @ {f.GetNativeOffset()}");
            }
            else
                logger.Add(@"Call stack was not recorded.");

        internal void NewFrame()
        {
            backgroundMonitorStackTrace = null;
        }

#pragma warning disable 0618
        private static StackTrace safelyGetStackTrace(Thread targetThread)
        {
            //code lifted from http://stackoverflow.com/a/14935378.
            //avoids deadlocks.

            using (ManualResetEvent fallbackThreadReady = new ManualResetEvent(false))
            using (ManualResetEvent exitedSafely = new ManualResetEvent(false))
            {
                Thread fallbackThread = new Thread(() =>
                {
                    // ReSharper disable AccessToDisposedClosure
                    fallbackThreadReady.Set();
                    while (!exitedSafely.WaitOne(200))
                    {
                        try
                        {
                            targetThread.Resume();
                        }
                        catch (Exception)
                        {
                            /*Whatever happens, do never stop to resume the target-thread regularly until the main-thread has exited safely.*/
                        }
                    }
                    // ReSharper restore AccessToDisposedClosure
                }) { Name = @"GetStackFallbackThread" };

                try
                {
                    fallbackThread.Start();
                    fallbackThreadReady.WaitOne();
                    //From here, you have about 200ms to get the stack-trace.
                    targetThread.Suspend();
                    StackTrace trace = null;
                    try
                    {
#pragma warning disable 612
                        trace = new StackTrace(targetThread, false);
#pragma warning restore 612
                    }
                    catch (ThreadStateException)
                    {
                        //failed to get stack trace, since the fallback-thread resumed the thread
                        //possible reasons:
                        //1.) This thread was just too slow (not very likely)
                        //2.) The deadlock ocurred and the fallbackThread rescued the situation.
                        //In both cases just return null.
                    }

                    try
                    {
                        targetThread.Resume();
                    }
                    catch (ThreadStateException)
                    {
                        /*Thread is running again already*/
                    }

                    return trace;
                }
                finally
                {
                    //Just signal the backup-thread to stop.
                    exitedSafely.Set();
                    //Join the thread to avoid disposing "exited safely" too early. And also make sure that no leftover threads are cluttering iis by accident.
                    fallbackThread.Join();
                }
            }
        }
#pragma warning restore 0618
    }
}
