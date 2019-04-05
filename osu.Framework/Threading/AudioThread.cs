// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using osu.Framework.Audio;

namespace osu.Framework.Threading
{
    public class AudioThread : GameThread
    {
        public AudioThread()
            : base(name: "Audio")
        {
            OnNewFrame = onNewFrame;
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

        private void onNewFrame()
        {
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
            ManagedBass.Bass.Free();
        }
    }
}
