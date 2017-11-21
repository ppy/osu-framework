// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Logging;
using osu.Framework.Timing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Runtime;

namespace osu.Framework.Statistics
{
    internal class BackgroundStackTraceCollector
    {
        private IList<ClrStackFrame> backgroundMonitorStackTrace;

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

            var backgroundMonitorThread = new Thread(() =>
            {
                while (true)
                {
                    if (targetThread.IsAlive && clock.ElapsedMilliseconds - LastConsumptionTime > SpikeRecordDuration && backgroundMonitorStackTrace == null)
                        backgroundMonitorStackTrace = getStackTrace(targetThread);

                    Thread.Sleep(1);
                }

                // ReSharper disable once FunctionNeverReturns
            }) { IsBackground = true };

            backgroundMonitorThread.Start();
        }

        internal void LogFrame(double elapsedFrameTime)
        {
            if (targetThread == null) return;

            var frames = backgroundMonitorStackTrace;
            backgroundMonitorStackTrace = null;

            StringBuilder logMessage = new StringBuilder();

            logMessage.AppendLine($@"---------- Slow Frame Detected on {targetThread.Name} at {clock.CurrentTime / 1000:#0.00}s ----------");

            logMessage.AppendLine();

            logMessage.AppendLine($@"Frame length: {elapsedFrameTime:#0,#}ms");

            if (frames != null)
            {
                logMessage.AppendLine(@"Call stack follows:");
                logMessage.AppendLine();

                foreach (var f in frames)
                    logMessage.AppendLine($@"- {f.DisplayString}");
            }
            else
                logMessage.AppendLine(@"Call stack was not recorded.");

            logMessage.AppendLine(@"------------------------------------------------------------------------------------------------------");

            logger.Add(logMessage.ToString());
        }

        internal void NewFrame()
        {
            backgroundMonitorStackTrace = null;
        }

        private static readonly Lazy<ClrInfo> clr_info = new Lazy<ClrInfo>(delegate
        {
            try
            {
                return DataTarget.AttachToProcess(Process.GetCurrentProcess().Id, 200, AttachFlag.Passive).ClrVersions[0];
            }
            catch
            {
                return null;
            }
        });

        private static IList<ClrStackFrame> getStackTrace(Thread targetThread) => clr_info.Value?.CreateRuntime().Threads.FirstOrDefault(t => t.ManagedThreadId == targetThread.ManagedThreadId)?.StackTrace;
    }
}
