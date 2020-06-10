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
        private readonly Dictionary<int, int> deviceReferences = new Dictionary<int, int>();

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
            Debug.Assert(IsCurrent);

            if (Bass.Init(deviceIndex) || Bass.LastError == Errors.Already)
            {
                // If the default (-1) device was initialised, we need to re-query for the device id that BASS mapped it to. For all other cases, this is a no-op.
                deviceIndex = Bass.CurrentDevice;
                deviceReferences[deviceIndex] = deviceReferences.GetValueOrDefault(deviceIndex) + 1;
                return true;
            }

            if (BassUtils.CheckFaulted(false))
                return false;

            Logger.Log("BASS failed to initialize but did not provide an error code", level: LogLevel.Error);
            return false;
        }

        internal void FreeDevice(int deviceIndex)
        {
            Debug.Assert(IsCurrent);

            if (deviceIndex == -1)
                return;

            Debug.Assert(deviceReferences[deviceIndex] > 0, $"{nameof(FreeDevice)} was called before {nameof(InitDevice)}.");

            if (--deviceReferences[deviceIndex] == 0)
            {
                // Since all references to the device have been removed, it is now safe to free the device.
                Bass.Free();
            }
        }

        protected override void PerformExit()
        {
            lock (managers)
            {
                foreach (var manager in managers)
                    manager.Dispose();
            }

            // The AudioManager disposal is scheduled, so we need to continue execution one last time.
            RunFrame();

            base.PerformExit();
        }
    }
}
