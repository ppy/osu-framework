// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using osu.Framework.Development;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Threading;

namespace osu.Framework.Logging
{
    /// <summary>
    /// This class allows statically (globally) configuring and using logging functionality.
    /// </summary>
    public class Logger
    {
        private static readonly object static_sync_lock = new object();

        // separate locking object for flushing so that we don't lock too long on the staticSyncLock object, since we have to
        // hold this lock for the entire duration of the flush (waiting for I/O etc) before we can resume scheduling logs
        // but other operations like GetLogger(), ApplyFilters() etc. can still be executed while a flush is happening.
        private static readonly object flush_sync_lock = new object();

        /// <summary>
        /// Whether logging is enabled. Setting this to false will disable all logging.
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// The minimum log-level a logged message needs to have to be logged. Default is <see cref="LogLevel.Verbose"/>. Please note that setting this to <see cref="LogLevel.Debug"/>  will log input events, including keypresses when entering a password.
        /// </summary>
        public static LogLevel Level = DebugUtils.IsDebugBuild ? LogLevel.Debug : LogLevel.Verbose;

        /// <summary>
        /// An identifier used in log file headers to figure where the log file came from.
        /// </summary>
        public static string UserIdentifier = Environment.UserName;

        /// <summary>
        /// An identifier for the game written to log file headers to indicate where the log file came from.
        /// </summary>
        public static string GameIdentifier = @"game";

        /// <summary>
        /// An identifier for the version written to log file headers to indicate where the log file came from.
        /// </summary>
        public static string VersionIdentifier = @"unknown";

        private static Storage storage;

        /// <summary>
        /// The storage to place logs inside.
        /// </summary>
        public static Storage Storage
        {
            private get => storage;
            set
            {
                storage = value ?? throw new ArgumentNullException(nameof(value));

                // clear static loggers so they are correctly purged at the new storage location.
                static_loggers.Clear();
            }
        }

        /// <summary>
        /// The target for which this logger logs information. This will only be null if the logger has a name.
        /// </summary>
        public LoggingTarget? Target { get; }

        /// <summary>
        /// The name of the logger.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the name of the file that this logger is logging to.
        /// </summary>
        public string Filename => $@"{Name}.log";

        public int TotalLogOperations => logCount.Value;

        private readonly GlobalStatistic<int> logCount;

        private static readonly HashSet<string> reserved_names = new HashSet<string>(Enum.GetNames(typeof(LoggingTarget)).Select(n => n.ToLowerInvariant()));

        private Logger(LoggingTarget target = LoggingTarget.Runtime)
            : this(target.ToString(), false)
        {
            Target = target;
        }

        private Logger(string name, bool checkedReserved)
        {
            name = name.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("The name of a logger must be non-null and may not contain only white space.", nameof(name));

            if (checkedReserved && reserved_names.Contains(name))
                throw new ArgumentException($"The name \"{name}\" is reserved. Please use the {nameof(LoggingTarget)}-value corresponding to the name instead.");

            Name = name;
            logCount = GlobalStatistics.Get<int>(nameof(Logger), Name);
        }

        /// <summary>
        /// Add a plain-text phrase which should always be filtered from logs. The filtered phrase will be replaced with asterisks (*).
        /// Useful for avoiding logging of credentials.
        /// See also <seealso cref="ApplyFilters(string)"/>.
        /// </summary>
        public static void AddFilteredText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            lock (static_sync_lock)
                filters.Add(text);
        }

        /// <summary>
        /// Removes phrases which should be filtered from logs.
        /// Useful for avoiding logging of credentials.
        /// See also <seealso cref="AddFilteredText(string)"/>.
        /// </summary>
        public static string ApplyFilters(string message)
        {
            lock (static_sync_lock)
            {
                foreach (string f in filters)
                    message = message.Replace(f, string.Empty.PadRight(f.Length, '*'));
            }

            return message;
        }

        /// <summary>
        /// Logs the given exception with the given description to the specified logging target.
        /// </summary>
        /// <param name="e">The exception that should be logged.</param>
        /// <param name="description">The description of the error that should be logged with the exception.</param>
        /// <param name="target">The logging target (file).</param>
        /// <param name="recursive">Whether the inner exceptions of the given exception <paramref name="e"/> should be logged recursively.</param>
        public static void Error(Exception e, string description, LoggingTarget target = LoggingTarget.Runtime, bool recursive = false)
        {
            error(e, description, target, null, recursive);
        }

        /// <summary>
        /// Logs the given exception with the given description to the logger with the given name.
        /// </summary>
        /// <param name="e">The exception that should be logged.</param>
        /// <param name="description">The description of the error that should be logged with the exception.</param>
        /// <param name="name">The logger name (file).</param>
        /// <param name="recursive">Whether the inner exceptions of the given exception <paramref name="e"/> should be logged recursively.</param>
        public static void Error(Exception e, string description, string name, bool recursive = false)
        {
            error(e, description, null, name, recursive);
        }

        private static void error(Exception e, string description, LoggingTarget? target, string name, bool recursive)
        {
            log($@"{description}", target, name, LogLevel.Error, e);

            if (recursive && e.InnerException != null)
                error(e.InnerException, $"{description} (inner)", target, name, true);
        }

        /// <summary>
        /// Log an arbitrary string to the specified logging target.
        /// </summary>
        /// <param name="message">The message to log. Can include newline (\n) characters to split into multiple lines.</param>
        /// <param name="target">The logging target (file).</param>
        /// <param name="level">The verbosity level.</param>
        /// <param name="outputToListeners">Whether the message should be sent to listeners of <see cref="Debug"/> and <see cref="Console"/>. True by default.</param>
        public static void Log(string message, LoggingTarget target = LoggingTarget.Runtime, LogLevel level = LogLevel.Verbose, bool outputToListeners = true)
        {
            log(message, target, null, level, outputToListeners: outputToListeners);
        }

        /// <summary>
        /// Log an arbitrary string to the logger with the given name.
        /// </summary>
        /// <param name="message">The message to log. Can include newline (\n) characters to split into multiple lines.</param>
        /// <param name="name">The logger name (file).</param>
        /// <param name="level">The verbosity level.</param>
        /// <param name="outputToListeners">Whether the message should be sent to listeners of <see cref="Debug"/> and <see cref="Console"/>. True by default.</param>
        public static void Log(string message, string name, LogLevel level = LogLevel.Verbose, bool outputToListeners = true)
        {
            log(message, null, name, level, outputToListeners: outputToListeners);
        }

        private static void log(string message, LoggingTarget? target, string loggerName, LogLevel level, Exception exception = null, bool outputToListeners = true)
        {
            try
            {
                if (target.HasValue)
                    GetLogger(target.Value).Add(message, level, exception, outputToListeners);
                else
                    GetLogger(loggerName).Add(message, level, exception, outputToListeners);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Logs a message to the specified logging target and also displays a print statement.
        /// </summary>
        /// <param name="message">The message to log. Can include newline (\n) characters to split into multiple lines.</param>
        /// <param name="target">The logging target (file).</param>
        /// <param name="level">The verbosity level.</param>
        public static void LogPrint(string message, LoggingTarget target = LoggingTarget.Runtime, LogLevel level = LogLevel.Verbose)
        {
            if (Enabled && DebugUtils.IsDebugBuild)
                System.Diagnostics.Debug.Print(message);

            Log(message, target, level);
        }

        /// <summary>
        /// Logs a message to the logger with the given name and also displays a print statement.
        /// </summary>
        /// <param name="message">The message to log. Can include newline (\n) characters to split into multiple lines.</param>
        /// <param name="name">The logger name (file).</param>
        /// <param name="level">The verbosity level.</param>
        public static void LogPrint(string message, string name, LogLevel level = LogLevel.Verbose)
        {
            if (Enabled && DebugUtils.IsDebugBuild)
                System.Diagnostics.Debug.Print(message);

            Log(message, name, level);
        }

        /// <summary>
        /// For classes that regularly log to the same target, this method may be preferred over the static Log method.
        /// </summary>
        /// <param name="target">The logging target.</param>
        /// <returns>The logger responsible for the given logging target.</returns>
        public static Logger GetLogger(LoggingTarget target = LoggingTarget.Runtime) => GetLogger(target.ToString());

        /// <summary>
        /// For classes that regularly log to the same target, this method may be preferred over the static Log method.
        /// </summary>
        /// <param name="name">The name of the custom logger.</param>
        /// <returns>The logger responsible for the given logging target.</returns>
        public static Logger GetLogger(string name)
        {
            lock (static_sync_lock)
            {
                string nameLower = name.ToLowerInvariant();

                if (!static_loggers.TryGetValue(nameLower, out Logger l))
                {
                    static_loggers[nameLower] = l = Enum.TryParse(name, true, out LoggingTarget target) ? new Logger(target) : new Logger(name, true);
                    l.clear();
                }

                return l;
            }
        }

        /// <summary>
        /// Logs a new message with the <see cref="LogLevel.Debug"/> and will only be logged if your project is built in the Debug configuration. Please note that the default setting for <see cref="Level"/> is <see cref="LogLevel.Verbose"/> so unless you increase the <see cref="Level"/> to <see cref="LogLevel.Debug"/> messages printed with this method will not appear in the output.
        /// </summary>
        /// <param name="message">The message that should be logged.</param>
        public void Debug(string message = @"")
        {
            if (!DebugUtils.IsDebugBuild)
                return;

            Add(message, LogLevel.Debug);
        }

        /// <summary>
        /// Log an arbitrary string to current log.
        /// </summary>
        /// <param name="message">The message to log. Can include newline (\n) characters to split into multiple lines.</param>
        /// <param name="level">The verbosity level.</param>
        /// <param name="exception">An optional related exception.</param>
        /// <param name="outputToListeners">Whether the message should be sent to listeners of <see cref="Debug"/> and <see cref="Console"/>. True by default.</param>
        public void Add(string message = @"", LogLevel level = LogLevel.Verbose, Exception exception = null, bool outputToListeners = true) =>
            add(message, level, exception, outputToListeners && OutputToListeners);

        private readonly RollingTime debugOutputRollingTime = new RollingTime(50, 10000);

        private readonly Queue<string> pendingFileOutput = new Queue<string>();

        private void add(string message = @"", LogLevel level = LogLevel.Verbose, Exception exception = null, bool outputToListeners = true)
        {
            if (!Enabled || level < Level)
                return;

            ensureHeader();

            logCount.Value++;

            message = ApplyFilters(message);

            string logOutput = message;

            if (exception != null)
                // add exception output to console / logfile output (but not the LogEntry's message).
                logOutput += $"\n{ApplyFilters(exception.ToString())}";

            IEnumerable<string> lines = logOutput
                                        .Replace(@"\r\n", @"\n")
                                        .Split('\n')
                                        .Select(s => $@"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} [{level.ToString().ToLowerInvariant()}]: {s.Trim()}");

            if (outputToListeners)
            {
                NewEntry?.Invoke(new LogEntry
                {
                    Level = level,
                    Target = Target,
                    LoggerName = Name,
                    Message = message,
                    Exception = exception
                });

                if (DebugUtils.IsDebugBuild)
                {
                    static void consoleLog(string msg)
                    {
                        // fire to all debug listeners (like visual studio's output window)
                        System.Diagnostics.Debug.Print(msg);
                        // fire for console displays (appveyor/CI).
                        Console.WriteLine(msg);
                    }

                    bool bypassRateLimit = level >= LogLevel.Verbose;

                    foreach (string line in lines)
                    {
                        if (bypassRateLimit || debugOutputRollingTime.RequestEntry())
                        {
                            consoleLog($"[{Name.ToLowerInvariant()}] {line}");

                            if (!bypassRateLimit && debugOutputRollingTime.IsAtLimit)
                                consoleLog($"Console output is being limited. Please check {Filename} for full logs.");
                        }
                    }
                }
            }

            if (Target == LoggingTarget.Information)
                // don't want to log this to a file
                return;

            lock (flush_sync_lock)
            {
                // we need to check if the logger is still enabled here, since we may have been waiting for a
                // flush and while the flush was happening, the logger might have been disabled. In that case
                // we want to make sure that we don't accidentally write anything to a file after that flush.
                if (!Enabled)
                    return;

                lock (pendingFileOutput)
                {
                    foreach (string l in lines)
                        pendingFileOutput.Enqueue(l);
                }

                scheduler.AddOnce(writePendingLines);

                writer_idle.Reset();
            }
        }

        private void writePendingLines()
        {
            string[] lines;

            lock (pendingFileOutput)
            {
                lines = pendingFileOutput.ToArray();
                pendingFileOutput.Clear();
            }

            try
            {
                using (var stream = Storage.GetStream(Filename, FileAccess.Write, FileMode.Append))
                using (var writer = new StreamWriter(stream))
                {
                    foreach (string line in lines)
                        writer.WriteLine(line);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Whether the output of this logger should be sent to listeners of <see cref="Debug"/> and <see cref="Console"/>.
        /// Defaults to true.
        /// </summary>
        public bool OutputToListeners { get; set; } = true;

        /// <summary>
        /// Fires whenever any logger tries to log a new entry, but before the entry is actually written to the logfile.
        /// </summary>
        public static event Action<LogEntry> NewEntry;

        /// <summary>
        /// Deletes log file from disk.
        /// </summary>
        private void clear()
        {
            lock (flush_sync_lock)
            {
                scheduler.Add(() =>
                {
                    try
                    {
                        Storage.Delete(Filename);
                    }
                    catch
                    {
                        // may fail if the file/directory was already cleaned up, ie. during test runs.
                    }
                });
                writer_idle.Reset();
            }
        }

        private bool headerAdded;

        private void ensureHeader()
        {
            if (headerAdded) return;

            headerAdded = true;

            add("----------------------------------------------------------", outputToListeners: false);
            add($"{Name} Log for {UserIdentifier} (LogLevel: {Level})", outputToListeners: false);
            add($"Running {GameIdentifier} {VersionIdentifier} on .NET {Environment.Version}", outputToListeners: false);
            add($"Environment: {RuntimeInfo.OS} ({Environment.OSVersion}), {Environment.ProcessorCount} cores ", outputToListeners: false);
            add("----------------------------------------------------------", outputToListeners: false);
        }

        private static readonly List<string> filters = new List<string>();
        private static readonly Dictionary<string, Logger> static_loggers = new Dictionary<string, Logger>();

        private static readonly Scheduler scheduler = new Scheduler();

        private static readonly ManualResetEvent writer_idle = new ManualResetEvent(true);

        static Logger()
        {
            Timer timer = null;

            // timer has a very low overhead.
            timer = new Timer(_ =>
            {
                if ((Storage != null ? scheduler.Update() : 0) == 0)
                    writer_idle.Set();

                // reschedule every 50ms. avoids overlapping callbacks.
                // ReSharper disable once AccessToModifiedClosure
                timer?.Change(50, Timeout.Infinite);
            }, null, 0, Timeout.Infinite);
        }

        /// <summary>
        /// Pause execution until all logger writes have completed and file handles have been closed.
        /// This will also unbind all handlers bound to <see cref="NewEntry"/>.
        /// </summary>
        public static void Flush()
        {
            lock (flush_sync_lock)
            {
                writer_idle.WaitOne(2000);
                NewEntry = null;
            }
        }
    }

    /// <summary>
    /// Captures information about a logged message.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// The level for which the message was logged.
        /// </summary>
        public LogLevel Level;

        /// <summary>
        /// The target to which this message is being logged, or null if it is being logged to a custom named logger.
        /// </summary>
        public LoggingTarget? Target;

        /// <summary>
        /// The name of the logger to which this message is being logged, or null if it is being logged to a specific <see cref="LoggingTarget"/>.
        /// </summary>
        public string LoggerName;

        /// <summary>
        /// The message that was logged.
        /// </summary>
        public string Message;

        /// <summary>
        /// An optional related exception.
        /// </summary>
        public Exception Exception;
    }

    /// <summary>
    /// The level on which a log-message is logged.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Log-level for debugging-related log-messages. This is the lowest level (highest verbosity). Please note that this will log input events, including keypresses when entering a password.
        /// </summary>
        Debug,

        /// <summary>
        /// Log-level for most log-messages. This is the second-lowest level (second-highest verbosity).
        /// </summary>
        Verbose,

        /// <summary>
        /// Log-level for important log-messages. This is the second-highest level (second-lowest verbosity).
        /// </summary>
        Important,

        /// <summary>
        /// Log-level for error messages. This is the highest level (lowest verbosity).
        /// </summary>
        Error
    }

    /// <summary>
    /// The target for logging. Different targets can have different logfiles, are displayed differently in the LogOverlay and are generally useful for organizing logs into groups.
    /// </summary>
    public enum LoggingTarget
    {
        /// <summary>
        /// Logging target for general information. Everything logged with this target will not be written to a logfile.
        /// </summary>
        Information,

        /// <summary>
        /// Logging target for information about the runtime.
        /// </summary>
        Runtime,

        /// <summary>
        /// Logging target for network-related events.
        /// </summary>
        Network,

        /// <summary>
        /// Logging target for performance-related information.
        /// </summary>
        Performance,

        /// <summary>
        /// Logging target for database-related events.
        /// </summary>
        Database
    }
}
