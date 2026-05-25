// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Audio.EzLatency
{
#nullable disable

    public class EzLoggerAdapter : IEzLatencyLogger
    {
        private readonly Scheduler scheduler;
        private readonly StreamWriter fileWriter;
        public event Action<EzLatencyRecord> OnRecord;

        public EzLoggerAdapter(Scheduler scheduler = null, string filePath = null)
        {
            this.scheduler = scheduler;

            this.scheduler = scheduler;
            {
                try
                {
                    if (filePath != null) fileWriter = new StreamWriter(File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
                }
                catch (Exception ex)
                {
                    Logger.Log($"EzLoggerAdapter: failed to open file {filePath}: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                    fileWriter = null;
                }
            }
        }

        public void Log(EzLatencyRecord record)
        {
            try
            {
                if (scheduler != null)
                    scheduler.Add(() => OnRecord?.Invoke(record));
                else
                    OnRecord?.Invoke(record);
            }
            catch (Exception ex)
            {
                Logger.Log($"EzLoggerAdapter: OnRecord handler threw: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }

            try
            {
                if (fileWriter != null)
                {
                    string line = $"[{record.Timestamp:O}] {record.MeasuredMs} ms - {record.Note}";
                    fileWriter.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"EzLoggerAdapter: failed to write to file: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        public void Flush()
        {
            try
            {
                fileWriter?.Flush();
            }
            catch (Exception ex)
            {
                Logger.Log($"EzLoggerAdapter: flush failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        public void Dispose()
        {
            try
            {
                fileWriter?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"EzLoggerAdapter: dispose failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }
    }
}
