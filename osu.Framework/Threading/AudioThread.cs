// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using ManagedBass;
using osu.Framework.Audio;
using osu.Framework.Development;

namespace osu.Framework.Threading
{
    public class AudioThread : GameThread
    {
        public AudioThread()
            : base(name: "Audio")
        {
            OnNewFrame = onNewFrame;
        }

        public override bool IsCurrent => ThreadSafety.IsAudioThread;

        internal sealed override void MakeCurrent()
        {
            base.MakeCurrent();

            ThreadSafety.IsAudioThread = true;
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.TasksRun,
            StatisticsCounterType.Tracks,
            StatisticsCounterType.Samples,
            StatisticsCounterType.SChannels,
            StatisticsCounterType.Components,
        };

        private readonly List<AudioManager> managers = new List<AudioManager>();

        private static readonly GlobalStatistic<double> cpu_usage = GlobalStatistics.Get<double>("Audio", "Bass CPU%");

        private void onNewFrame()
        {
            cpu_usage.Value = Bass.CPUUsage;

            lock (managers)
            {
                for (var i = 0; i < managers.Count; i++)
                {
                    var m = managers[i];
                    m.Update();
                }
            }
        }

        public void RegisterManager(AudioManager manager)
        {
            lock (managers)
            {
                if (managers.Contains(manager))
                    throw new InvalidOperationException($"{manager} was already registered");

                managers.Add(manager);
            }
        }

        public void UnregisterManager(AudioManager manager)
        {
            lock (managers)
                managers.Remove(manager);
        }

        protected override void PerformExit()
        {
            base.PerformExit();

            lock (managers)
            {
                foreach (var manager in managers)
                    manager.Dispose();
                managers.Clear();
            }

            // Safety net to ensure we have freed all devices before exiting.
            // This is mainly required for device-lost scenarios.
            // See https://github.com/ppy/osu-framework/pull/3378 for further discussion.
            while (Bass.Free()) { }
        }
    }
}
