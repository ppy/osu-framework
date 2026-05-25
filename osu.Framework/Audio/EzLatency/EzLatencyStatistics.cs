// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Audio.EzLatency
{
#nullable disable

    public class EzLatencyStatistics
    {
        public bool HasData => RecordCount > 0;
        public int RecordCount { get; set; }
        public double AvgInputToJudge { get; set; }
        public double AvgInputToPlayback { get; set; }
        public double AvgPlaybackToJudge { get; set; }
        public double MinInputToJudge { get; set; }
        public double MaxInputToJudge { get; set; }
        public double AvgHardwareLatency { get; set; }
    }

    internal class EzLatencyCollector
    {
        private readonly List<EzLatencyRecord> records = new List<EzLatencyRecord>();
        private readonly object lockObject = new object();

        public void AddRecord(EzLatencyRecord record)
        {
            // Accept records if input data is valid (best-effort), even if hardware data isn't available.
            if (!record.InputData.IsValid)
                return;

            lock (lockObject)
            {
                records.Add(record);
            }
        }

        public void Clear()
        {
            lock (lockObject)
            {
                records.Clear();
            }
        }

        public EzLatencyStatistics GetStatistics()
        {
            lock (lockObject)
            {
                if (records.Count == 0)
                    return new EzLatencyStatistics { RecordCount = 0 };

                // Filter out invalid or incomplete differences (<= 0) to avoid extreme/garbage values.
                var inputToJudge = records
                                   .Where(r => r.InputTime > 0 && r.JudgeTime > 0)
                                   .Select(r => r.JudgeTime - r.InputTime)
                                   .Where(d => Math.Abs(d) <= 1000) // sanity cap: ignore absurdly large diffs (>1000ms)
                                   .ToList();

                var inputToPlayback = records
                                      .Where(r => r.InputTime > 0 && r.PlaybackTime > 0)
                                      .Select(r => r.PlaybackTime - r.InputTime)
                                      .Where(d => Math.Abs(d) <= 1000)
                                      .ToList();

                var playbackToJudge = records
                                      .Where(r => r.PlaybackTime > 0 && r.JudgeTime > 0)
                                      .Select(r => r.JudgeTime - r.PlaybackTime)
                                      .Where(d => Math.Abs(d) <= 1000)
                                      .ToList();

                var hardwareLatency = records.Select(r => r.OutputHardwareTime).Where(h => h > 0).ToList();

                double avgInputToJudge = inputToJudge.Count > 0 ? inputToJudge.Average() : 0;
                double avgInputToPlayback = inputToPlayback.Count > 0 ? inputToPlayback.Average() : 0;
                double avgPlaybackToJudge = playbackToJudge.Count > 0 ? playbackToJudge.Average() : 0;
                double minInputToJudge = inputToJudge.Count > 0 ? inputToJudge.Min() : 0;
                double maxInputToJudge = inputToJudge.Count > 0 ? inputToJudge.Max() : 0;

                return new EzLatencyStatistics
                {
                    RecordCount = records.Count,
                    AvgInputToJudge = avgInputToJudge,
                    AvgInputToPlayback = avgInputToPlayback,
                    AvgPlaybackToJudge = avgPlaybackToJudge,
                    MinInputToJudge = minInputToJudge,
                    MaxInputToJudge = maxInputToJudge,
                    AvgHardwareLatency = hardwareLatency.Count > 0 ? hardwareLatency.Average() : 0
                };
            }
        }

        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return records.Count;
                }
            }
        }
    }
}
