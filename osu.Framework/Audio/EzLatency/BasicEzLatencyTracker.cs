// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.EzLatency
{
#nullable disable

    public class BasicEzLatencyTracker : IEzLatencyTracker
    {
        public event Action<EzLatencyRecord> OnMeasurement;
        private readonly EzLatencyAnalyzer analyzer;
        private readonly Action<EzLatencyRecord> recordHandler;

        public BasicEzLatencyTracker()
        {
            analyzer = new EzLatencyAnalyzer();
            recordHandler = record => OnMeasurement?.Invoke(record);
            analyzer.OnNewRecord += recordHandler;
        }

        public void Start() { analyzer.Enabled = true; }
        public void Stop() { analyzer.Enabled = false; }
        public void SetSampleRate(int sampleRate) { }
        public void PushMeasurement(EzLatencyRecord record) => OnMeasurement?.Invoke(record);

        public void Dispose()
        {
            analyzer.Enabled = false;
            if (recordHandler != null) analyzer.OnNewRecord -= recordHandler;
        }

        public void RecordInputData(double inputTime, object keyValue = null) => analyzer.RecordInputData(inputTime, keyValue);
        public void RecordJudgeData(double judgeTime) => analyzer.RecordJudgeData(judgeTime);
        public void RecordPlaybackData(double playbackTime) => analyzer.RecordPlaybackData(playbackTime);

        public void RecordHardwareData(double driverTime, double outputHardwareTime, double inputHardwareTime, double latencyDifference) =>
            analyzer.RecordHardwareData(driverTime, outputHardwareTime, inputHardwareTime, latencyDifference);
    }
}
