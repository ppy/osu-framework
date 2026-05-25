// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Logging;

namespace osu.Framework.Audio.EzLatency
{
#nullable disable

    public class EzLatencyAnalyzer
    {
        private readonly Stopwatch stopwatch;
        public bool Enabled { get; set; }
        public event Action<EzLatencyRecord> OnNewRecord;

        private EzLatencyInputData currentInputData;
        private EzLatencyHardwareData currentHardwareData;
        private double recordStartTime;
        private const double timeout_ms = 5000;

        public EzLatencyAnalyzer()
        {
            stopwatch = Stopwatch.StartNew();
        }

        public void RecordInputData(double inputTime, object keyValue = null)
        {
            if (!Enabled) return;

            if (currentInputData.InputTime > 0)
            {
                currentInputData = default;
                currentHardwareData = default;
            }

            currentInputData.InputTime = inputTime;
            currentInputData.KeyValue = keyValue;
            recordStartTime = stopwatch.Elapsed.TotalMilliseconds;
        }

        public void RecordJudgeData(double judgeTime)
        {
            if (!Enabled) return;

            currentInputData.JudgeTime = judgeTime;
            checkTimeout();
        }

        public void RecordPlaybackData(double playbackTime)
        {
            if (!Enabled) return;

            currentInputData.PlaybackTime = playbackTime;
            tryGenerateCompleteRecord();
        }

        public void RecordHardwareData(double driverTime, double outputHardwareTime, double inputHardwareTime, double latencyDifference)
        {
            if (!Enabled) return;

            currentHardwareData = new EzLatencyHardwareData
            {
                DriverTime = driverTime,
                OutputHardwareTime = outputHardwareTime,
                InputHardwareTime = inputHardwareTime,
                LatencyDifference = latencyDifference
            };

            tryGenerateCompleteRecord();
        }

        private void tryGenerateCompleteRecord()
        {
            if (!currentInputData.IsValid)
            {
                checkTimeout();
                return;
            }

            // If we have hardware data, emit a full record. Otherwise emit a best-effort record without hw.
            var record = new EzLatencyRecord
            {
                Timestamp = DateTimeOffset.Now,
                InputTime = currentInputData.InputTime,
                JudgeTime = currentInputData.JudgeTime,
                PlaybackTime = currentInputData.PlaybackTime,
                DriverTime = currentHardwareData.DriverTime,
                OutputHardwareTime = currentHardwareData.OutputHardwareTime,
                InputHardwareTime = currentHardwareData.InputHardwareTime,
                LatencyDifference = currentHardwareData.LatencyDifference,
                // MeasuredMs: prefer Playback - Input when available, otherwise use Judge - Input as a best-effort.
                MeasuredMs = currentInputData.PlaybackTime > 0
                    ? currentInputData.PlaybackTime - currentInputData.InputTime
                    : currentInputData.JudgeTime > 0
                        ? currentInputData.JudgeTime - currentInputData.InputTime
                        : 0,
                Note = currentHardwareData.IsValid ? "complete-latency-measurement" : "best-effort-no-hw",
                InputData = currentInputData,
                HardwareData = currentHardwareData
            };

            try
            {
                OnNewRecord?.Invoke(record);
                EzLatencyService.Instance.PushRecord(record);
                Logger.Log(
                    currentHardwareData.IsValid
                        ? $"EzLatency 完整记录已生成: Input→Playback={record.PlaybackTime - record.InputTime:F2}ms"
                        : $"EzLatency 最佳尝试记录（无硬件时间戳）: Input→Playback={record.PlaybackTime - record.InputTime:F2}ms", LoggingTarget.Runtime, LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"EzLatencyAnalyzer: tryGenerateCompleteRecord failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }

            ClearCurrentData();
        }

        public double GetCurrentTimestamp() => stopwatch.Elapsed.TotalMilliseconds;

        private void checkTimeout()
        {
            if (recordStartTime > 0)
            {
                double elapsed = stopwatch.Elapsed.TotalMilliseconds - recordStartTime;

                if (elapsed > timeout_ms)
                {
                    Logger.Log($"EzLatency 数据收集超时 ({elapsed:F0}ms)，清除旧数据", LoggingTarget.Runtime, LogLevel.Debug);
                    ClearCurrentData();
                }
            }
        }

        public void ClearCurrentData()
        {
            currentInputData = default;
            currentHardwareData = default;
            recordStartTime = 0;
        }
    }
}
