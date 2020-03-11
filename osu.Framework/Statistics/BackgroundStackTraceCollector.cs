// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Logging;
using osu.Framework.Timing;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace osu.Framework.Statistics
{
    /// <summary>
    /// Spawn a thread to collect real-time stack traces of the targeted thread.
    /// </summary>
    internal class BackgroundStackTraceCollector : IDisposable
    {
        private IList<ClrStackFrame> backgroundMonitorStackTrace;

        private readonly StopwatchClock clock;
        private readonly string threadName;

        private readonly Lazy<Logger> logger;

        private Thread targetThread;

        internal double LastConsumptionTime;

        private double spikeRecordThreshold;

        private bool enabled;

        /// <summary>
        /// Create a collector for the target thread. Starts in a disabled state (see <see cref="Enabled"/>.
        /// </summary>
        /// <param name="targetThread">The thread to monitor.</param>
        /// <param name="clock">The clock to use for elapsed time checks.</param>
        /// <param name="threadName">A name used for tracking purposes. Can be used to track potentially changing threads under a single name.</param>
        public BackgroundStackTraceCollector(Thread targetThread, StopwatchClock clock, string threadName = null)
        {
            if (Debugger.IsAttached)
                return;

            this.clock = clock;
            this.threadName = threadName ?? targetThread?.Name;
            this.targetThread = targetThread;

            logger = new Lazy<Logger>(() =>
            {
                var l = Logger.GetLogger($"performance-{threadName?.ToLower() ?? "unknown"}");
                l.OutputToListeners = false;
                return l;
            });
        }

        /// <summary>
        /// Whether this collector is currently running.
        /// </summary>
        public bool Enabled
        {
            get => enabled;
            set
            {
                if (value == enabled || targetThread == null) return;

                enabled = value;
                if (enabled)
                    startThread();
                else
                    stopThread();
            }
        }

        private CancellationTokenSource cancellation;

        private void startThread()
        {
            Trace.Assert(cancellation == null);

            var thread = new Thread(() => run((cancellation = new CancellationTokenSource()).Token))
            {
                Name = $"{threadName}-StackTraceCollector",
                IsBackground = true
            };

            thread.Start();
        }

        private bool isCollecting;

        private void run(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                var elapsed = clock.ElapsedMilliseconds - LastConsumptionTime;
                var threshold = spikeRecordThreshold / 2;

                if (targetThread.IsAlive && isCollecting && clock.ElapsedMilliseconds - LastConsumptionTime > spikeRecordThreshold / 2 && backgroundMonitorStackTrace == null)
                {
                    try
                    {
                        Logger.Log($"Retrieving background stack trace on {threadName} thread ({elapsed:N0}ms over threshold of {threshold:N0}ms)...");
                        backgroundMonitorStackTrace = getStackTrace(targetThread);
                        Logger.Log("Done!");

                        Thread.Sleep(100);
                    }
                    catch (Exception e)
                    {
                        Enabled = false;
                        Logger.Log($"Failed to retrieve background stack trace: {e}");
                    }
                }

                Thread.Sleep(5);
            }
        }

        private void stopThread()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
        }

        internal void NewFrame(double elapsedFrameTime, double newSpikeThreshold)
        {
            if (targetThread == null) return;

            isCollecting = true;

            var frames = backgroundMonitorStackTrace;
            backgroundMonitorStackTrace = null;

            var currentThreshold = spikeRecordThreshold;

            spikeRecordThreshold = newSpikeThreshold;

            if (!enabled || elapsedFrameTime < currentThreshold || currentThreshold == 0)
                return;

            StringBuilder logMessage = new StringBuilder();

            logMessage.AppendLine($@"| Slow frame on thread ""{threadName}""");
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

            logger.Value.Add(logMessage.ToString());
        }

        public void EndFrame()
        {
            isCollecting = false;
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

        private static IList<ClrStackFrame> getStackTrace(Thread targetThread) =>
            clr_info.Value?.CreateRuntime().Threads.FirstOrDefault(t => t.ManagedThreadId == targetThread.ManagedThreadId)?.StackTrace;

        #region IDisposable Support

        ~BackgroundStackTraceCollector()
        {
            Dispose(false);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                Enabled = false; // stops the thread if running.
                isDisposed = true;
                targetThread = null;
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
