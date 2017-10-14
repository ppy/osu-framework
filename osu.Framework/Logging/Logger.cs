// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Logging
{
    public class Logger
    {
        /// <summary>
        /// Global control over logging.
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// An identifier used in log file headers to figure where the log file came from.
        /// </summary>
        public static string UserIdentifier = Environment.UserName;

        /// <summary>
        /// An identifier for game used in log file headers to figure where the log file came from.
        /// </summary>
        public static string GameIdentifier = @"game";

        /// <summary>
        /// An identifier for version used in log file headers to figure where the log file came from.
        /// </summary>
        public static string VersionIdentifier = @"unknown";

        /// <summary>
        /// The storage to place logs inside.
        /// </summary>
        public static Storage Storage;

        /// <summary>
        /// Add a plain-text phrase which should always be filtered from logs.
        /// Useful for avoiding logging of credentials.
        /// </summary>
        public static void AddFilteredText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            filters.Add(text);
        }

        /// <summary>
        /// Removes phrases which should be filtered from logs.
        /// Useful for avoiding logging of credentials.
        /// </summary>
        public static string ApplyFilters(string message)
        {
            foreach (string f in filters)
                message = message.Replace(f, string.Empty.PadRight(f.Length, '*'));

            return message;
        }

        public static void Error(Exception e, string description, LoggingTarget target = LoggingTarget.Runtime, bool recursive = false)
        {
            Log($@"ERROR: {description}", target, LogLevel.Error);
            Log(e.ToString(), target, LogLevel.Error);

            if (recursive)
                for (Exception inner = e.InnerException; inner != null; inner = inner.InnerException)
                    Log(inner.ToString(), target, LogLevel.Error);
        }

        /// <summary>
        /// Log an arbitrary string to a specific log target.
        /// </summary>
        /// <param name="message">The message to log. Can include newline (\n) characters to split into multiple lines.</param>
        /// <param name="target">The logging target (file).</param>
        /// <param name="level">The verbosity level.</param>
        public static void Log(string message, LoggingTarget target = LoggingTarget.Runtime, LogLevel level = LogLevel.Verbose)
        {
            try
            {
                GetLogger(target, true).Add(message, level);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Logs a message to the given log target and also displays a print statement.
        /// </summary>
        /// <param name="message">The message to log. Can include newline (\n) characters to split into multiple lines.</param>
        /// <param name="target">The logging target (file).</param>
        /// <param name="level">The verbosity level.</param>
        public static void LogPrint(string message, LoggingTarget target = LoggingTarget.Runtime, LogLevel level = LogLevel.Verbose)
        {
#if DEBUG
            System.Diagnostics.Debug.Print(message);
#endif
            Log(message, target, level);
        }

        /// <summary>
        /// For classes that regularly log to the same target, this method may be preferred over the static Log method.
        /// </summary>
        /// <param name="target">The logging target.</param>
        /// <param name="clearOnConstruct">Decides whether we clear any existing content from the log the first time we construct this logger.</param>
        /// <returns></returns>
        public static Logger GetLogger(LoggingTarget target = LoggingTarget.Runtime, bool clearOnConstruct = false)
        {
            Logger l;
            if (!static_loggers.TryGetValue(target, out l))
            {
                static_loggers[target] = l = new Logger(target);
                if (clearOnConstruct) l.Clear();
            }

            return l;
        }

        public LoggingTarget Target { get; }

        public string Filename => $@"{Target.ToString().ToLower()}.log";

        private Logger(LoggingTarget target = LoggingTarget.Runtime)
        {
            Target = target;
        }

        [Conditional("DEBUG")]
        public void Debug(string message = @"")
        {
            Add(message);
        }

        /// <summary>
        /// Log an arbitrary string to current log.
        /// </summary>
        /// <param name="message">The message to log. Can include newline (\n) characters to split into multiple lines.</param>
        /// <param name="level">The verbosity level.</param>
        public void Add(string message = @"", LogLevel level = LogLevel.Verbose)
        {
#if DEBUG
            var debugLine = $"[{Target.ToString().ToLower()}:{level.ToString().ToLower()}] {message}";
            // fire to all debug listeners (like visual studio's output window)
            System.Diagnostics.Debug.Print(debugLine);
            // fire for console displays (appveyor/CI).
            Console.WriteLine(debugLine);
#endif

#if Public
            if (level < LogLevel.Important) return;
#endif

#if !DEBUG
            if (level <= LogLevel.Debug) return;
#endif

            if (!Enabled) return;

            message = ApplyFilters(message);

            //split each line up.
            string[] lines = message.TrimEnd().Replace(@"\r\n", @"\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string s = lines[i];
                lines[i] = $@"{DateTime.UtcNow.ToString(NumberFormatInfo.InvariantInfo)}: {s.Trim()}";
            }

            LogEntry entry = new LogEntry
            {
                Level = level,
                Target = Target,
                Message = message
            };

            NewEntry?.Invoke(entry);

            if (Target == LoggingTarget.Information)
                // don't want to log this to a file
                return;

            background_scheduler.Add(delegate
            {
                if (Storage == null)
                    return;

                try
                {
                    using (var stream = Storage.GetStream(Filename, FileAccess.Write, FileMode.Append))
                    using (var writer = new StreamWriter(stream))
                        foreach (var line in lines)
                            writer.WriteLine(line);
                }
                catch
                {
                }
            });
        }

        public static event Action<LogEntry> NewEntry;

        /// <summary>
        /// Deletes log file from disk.
        /// </summary>
        public void Clear()
        {
            background_scheduler.Add(() => Storage?.Delete(Filename));
            addHeader();
        }

        private void addHeader()
        {
            Add(@"----------------------------------------------------------");
            Add($@"{Target} Log for {UserIdentifier}");
            Add($@"{GameIdentifier} version {VersionIdentifier}");
            Add($@"Running on {Environment.OSVersion}, {Environment.ProcessorCount} cores");
            Add(@"----------------------------------------------------------");
        }

        private static readonly List<string> filters = new List<string>();
        private static readonly Dictionary<LoggingTarget, Logger> static_loggers = new Dictionary<LoggingTarget, Logger>();
        private static readonly ThreadedScheduler background_scheduler = new ThreadedScheduler(@"Logger");
    }

    public class LogEntry
    {
        public LogLevel Level;
        public LoggingTarget Target;
        public string Message;
    }

    public enum LogLevel
    {
        Debug,
        Verbose,
        Important,
        Error,
    }

    public enum LoggingTarget
    {
        Information,
        Runtime,
        Network,
        Tournament,
        Performance,
        Debug,
        Database
    }
}
