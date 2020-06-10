// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ManagedBass;
using osu.Framework.Audio;
using osu.Framework.Development;
using osu.Framework.Logging;
using osu.Framework.Platform.Linux.Native;

namespace osu.Framework.Threading
{
    public class AudioThread : GameThread
    {
        public AudioThread()
            : base(name: "Audio")
        {
            OnNewFrame = onNewFrame;

            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
            {
                // required for the time being to address libbass_fx.so load failures (see https://github.com/ppy/osu/issues/2852)
                Library.Load("libbass.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
            }
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
        private readonly HashSet<int> initialisedDevices = new HashSet<int>();

        private static readonly GlobalStatistic<double> cpu_usage = GlobalStatistics.Get<double>("Audio", "Bass CPU%");

        private void onNewFrame()
        {
            cpu_usage.Value = Bass.CPUUsage;

            lock (managers)
            {
                for (var i = 0; i < managers.Count; i++)
                    managers[i].Update();
            }
        }

        internal void RegisterManager(AudioManager manager)
        {
            lock (managers)
            {
                if (managers.Contains(manager))
                    throw new InvalidOperationException($"{manager} was already registered");

                managers.Add(manager);
            }
        }

        internal void UnregisterManager(AudioManager manager)
        {
            lock (managers)
                managers.Remove(manager);
        }

        internal bool InitDevice(int deviceIndex)
        {
            Debug.Assert(deviceIndex != -1, "Must not initialise with the default (-1) device.");
            Debug.Assert(IsCurrent, "Cannot initialise BASS when not on the audio thread.");

            if (Bass.Init(deviceIndex))
            {
                // BASS will switch the current device to the newly-initialised device when BASS_Init() succeeds.
                // Mark the device as having been initialised.
                initialisedDevices.Add(deviceIndex);
                return true;
            }

            if (Bass.LastError == Errors.Already)
            {
                // If the device has already been initialised, we can still use it, but we need to switch to the device first.
                Bass.CurrentDevice = deviceIndex;
                return true;
            }

            if (BassUtils.CheckFaulted(false))
                return false;

            Logger.Log("BASS failed to initialize but did not provide an error code", level: LogLevel.Error);
            return false;
        }

        protected override void PerformExit()
        {
            lock (managers)
            {
                foreach (var manager in managers)
                    manager.Dispose();
            }

            // The AudioManager disposal is scheduled, so we need to continue execution one last time.
            // This will let any final actions execute before the disposal is finalised and the devices can be cleaned up.
            RunFrame();

            // Clean up all previously-initialised devices in turn.
            foreach (var d in initialisedDevices)
            {
                Bass.CurrentDevice = d;
                Bass.Free();
            }

            initialisedDevices.Clear();

            base.PerformExit();
        }
    }
}
