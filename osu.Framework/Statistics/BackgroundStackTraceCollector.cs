﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
    /// <summary>
    /// Spwan a thread to collect real-time stack traces of the targeted thread.
    /// </summary>
    internal class BackgroundStackTraceCollector
    {
        private IList<ClrStackFrame> backgroundMonitorStackTrace;

        private readonly StopwatchClock clock;

        private readonly Logger logger;
        private readonly Thread targetThread;

        internal double LastConsumptionTime;

        private double spikeRecordThreshold;

        public BackgroundStackTraceCollector(Thread targetThread, StopwatchClock clock)
        {
            if (Debugger.IsAttached) return;

            logger = Logger.GetLogger($"performance-{targetThread.Name?.ToLower() ?? "unknown"}");
            logger.OutputToListeners = false;

            this.clock = clock;
            this.targetThread = targetThread;

            var backgroundMonitorThread = new Thread(() =>
            {
                while (true)
                {
                    if (targetThread.IsAlive && clock.ElapsedMilliseconds - LastConsumptionTime > spikeRecordThreshold / 2 && backgroundMonitorStackTrace == null)
                        backgroundMonitorStackTrace = getStackTrace(targetThread);

                    Thread.Sleep(1);
                }

                // ReSharper disable once FunctionNeverReturns
            }) { IsBackground = true };

            backgroundMonitorThread.Start();
        }

        internal void NewFrame(double elapsedFrameTime, double newSpikeThreshold)
        {
            if (targetThread == null) return;

            var frames = backgroundMonitorStackTrace;
            backgroundMonitorStackTrace = null;

            var currentThreshold = spikeRecordThreshold;

            spikeRecordThreshold = newSpikeThreshold;

            if (elapsedFrameTime < currentThreshold || currentThreshold == 0)
                return;

            StringBuilder logMessage = new StringBuilder();

            logMessage.AppendLine($@"| Slow frame on thread ""{targetThread.Name}""");
            logMessage.AppendLine(@"|");
            logMessage.AppendLine($@"| * Thread time  : {clock.CurrentTime:#0,#}ms");
            logMessage.AppendLine($@"| * Frame length : {elapsedFrameTime:#0,#}ms (allowable: {currentThreshold:#0,#}ms)");

            logMessage.AppendLine(@"|");

            if (frames != null)
            {
                logMessage.AppendLine(@"| Stack trace:");

                foreach (var f in frames)
                    logMessage.AppendLine($@"|- {f.DisplayString}");
            }
            else
                logMessage.AppendLine(@"| Call stack was not recorded.");

            logger.Add(logMessage.ToString());
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
