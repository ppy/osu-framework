// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Runtime;
using osu.Framework.Logging;
using osu.Framework.Timing;

namespace osu.Framework.Statistics
{
    /// <summary>
    /// Spawn a thread to collect real-time stack traces of the targeted thread.
    /// </summary>
    internal class BackgroundStackTraceCollector : IDisposable
    {
        private string[] backgroundMonitorStackTrace;

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
                var l = Logger.GetLogger($"performance-{threadName?.ToLowerInvariant() ?? "unknown"}");
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
            // Since v2.0 of Microsoft.Diagnostics.Runtime, support is provided to retrieve stack traces on unix platforms but
            // it causes a full core dump, which is very slow and causes a visible freeze.
            // For the time being let's remain windows-only (as this functionality used to be).

            // As it turns out, it's also too slow to be useful on windows, so let's fully disable for the time being.

            //if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
            //    return;

            /*Trace.Assert(cancellation == null);

            var thread = new Thread(() => run((cancellation = new CancellationTokenSource()).Token))
            {
                Name = $"{threadName}-StackTraceCollector",
                IsBackground = true
            };

            thread.Start();*/
        }

        private bool isCollecting;

        private void run(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                double elapsed = clock.ElapsedMilliseconds - LastConsumptionTime;
                double threshold = spikeRecordThreshold / 2;

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

            string[] frames = backgroundMonitorStackTrace;
            backgroundMonitorStackTrace = null;

            double currentThreshold = spikeRecordThreshold;

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

                foreach (string f in frames)
                    logMessage.AppendLine($@"|- {f}");
            }
            else
                logMessage.AppendLine(@"| Call stack was not recorded.");

            logger.Value.Add(logMessage.ToString());
        }

        public void EndFrame()
        {
            isCollecting = false;
        }

        private static string[] getStackTrace(Thread targetThread)
        {
            try
            {
#if NET6_0_OR_GREATER
                using (var target = DataTarget.CreateSnapshotAndAttach(Environment.ProcessId))
#else
                using (var target = DataTarget.CreateSnapshotAndAttach(Process.GetCurrentProcess().Id))
#endif
                {
                    using (var runtime = target.ClrVersions[0].CreateRuntime())
                    {
                        return runtime.Threads
                                      .FirstOrDefault(t => t.ManagedThreadId == targetThread.ManagedThreadId)?
                                      .EnumerateStackTrace().Select(f => f.ToString())
                                      .ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

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
