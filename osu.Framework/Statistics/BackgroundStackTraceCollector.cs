// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Logging;
using osu.Framework.Timing;
using System.Diagnostics;
using System.Text;
using System.Threading;

#if NET_FRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime;
#endif

namespace osu.Framework.Statistics
{
    /// <summary>
    /// Spwan a thread to collect real-time stack traces of the targeted thread.
    /// </summary>
    internal class BackgroundStackTraceCollector : IDisposable
    {
#if NET_FRAMEWORK
        private IList<ClrStackFrame> backgroundMonitorStackTrace;
#endif

        private readonly StopwatchClock clock;

        private readonly Logger logger;
        private readonly Thread targetThread;

        internal double LastConsumptionTime;

        private double spikeRecordThreshold;

        private readonly CancellationTokenSource cancellationToken;

        public bool Enabled = true;

        public BackgroundStackTraceCollector(Thread targetThread, StopwatchClock clock)
        {
            if (Debugger.IsAttached) return;

            logger = Logger.GetLogger($"performance-{targetThread.Name?.ToLower() ?? "unknown"}");
            logger.OutputToListeners = false;

            this.clock = clock;
            this.targetThread = targetThread;

            Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
#if NET_FRAMEWORK
                    if (Enabled && targetThread.IsAlive && clock.ElapsedMilliseconds - LastConsumptionTime > spikeRecordThreshold / 2 && backgroundMonitorStackTrace == null)
                        backgroundMonitorStackTrace = getStackTrace(targetThread);

#endif
                    Thread.Sleep(1);
                }
            }, (cancellationToken = new CancellationTokenSource()).Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        internal void NewFrame(double elapsedFrameTime, double newSpikeThreshold)
        {
            if (targetThread == null) return;

#if NET_FRAMEWORK
            var frames = backgroundMonitorStackTrace;
            backgroundMonitorStackTrace = null;
#endif

            var currentThreshold = spikeRecordThreshold;

            spikeRecordThreshold = newSpikeThreshold;

            if (!Enabled || elapsedFrameTime < currentThreshold || currentThreshold == 0)
                return;

            StringBuilder logMessage = new StringBuilder();

            logMessage.AppendLine($@"| Slow frame on thread ""{targetThread.Name}""");
            logMessage.AppendLine(@"|");
            logMessage.AppendLine($@"| * Thread time  : {clock.CurrentTime:#0,#}ms");
            logMessage.AppendLine($@"| * Frame length : {elapsedFrameTime:#0,#}ms (allowable: {currentThreshold:#0,#}ms)");

            logMessage.AppendLine(@"|");

#if NET_FRAMEWORK
            if (frames != null)
            {
                logMessage.AppendLine(@"| Stack trace:");

                foreach (var f in frames)
                    logMessage.AppendLine($@"|- {f.DisplayString}");
            }
            else
#endif
                logMessage.AppendLine(@"| Call stack was not recorded.");

            logger.Add(logMessage.ToString());
        }

#if NET_FRAMEWORK
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
#endif

        #region IDisposable Support

        ~BackgroundStackTraceCollector()
        {
            Dispose(false);

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                cancellationToken?.Cancel();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        }
    }
}
