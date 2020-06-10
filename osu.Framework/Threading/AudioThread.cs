// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

                managers.RemoveAll(m => m.IsDisposed);
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

        internal bool InitDevice(int deviceIndex)
        {
            Debug.Assert(deviceIndex != -1, "Must not initialise with the default (-1) device.");
            Debug.Assert(IsCurrent, "Cannot initialise devices when not on the audio thread.");

            if (Bass.Init(deviceIndex))
            {
                // BASS will switch the current device to the newly-initialised device when BASS_Init() succeeds.
                // Mark the device as having been initialised.
                deviceReferences[deviceIndex] = 1;
                return true;
            }

            if (Bass.LastError == Errors.Already)
            {
                // If the device has already been initialised, we can still use it, but we need to switch to the device first.
                Bass.CurrentDevice = deviceIndex;

                // See: FreeDevice(). The no-sound device is not freed, so it's possible to get here without it having been initialised locally.
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
            Debug.Assert(IsCurrent, "Cannot free devices when not on the audio thread.");

            // Can happen if the AudioManager is disposed before it ever initialised a device.
            if (deviceIndex == -1)
                return;

            // Check if we were the ones who initialised the device.
            if (!deviceReferences.ContainsKey(deviceIndex))
            {
                // I don't really know how this is possible - it's not a 100% replication but sometimes AudioManager
                // can be disposed while having a current device (0) without ever having initialised it.
                // This can result in a memory leak however I've only ever seen this happen within tests, so let's silently ignore this device.
                return;
            }

            if (deviceIndex == 0)
            {
                // Freeing the no-sound device in one thread seems to cause Bass_ChannelPlay() to hang indefinitely in other threads that are also using the no-sound device.
                // This may be a Linux-specific issue.
                // Once again, I've only seen this happen within tests and should not occur within standard execution, so let's silently ignore the no-sound device (it's only used for decoding anyway).
                return;
            }

            if (--deviceReferences[deviceIndex] == 0)
            {
                // All references have been lost to the device, so we can free it.
                // We'll need to activate the device to do so, but afterwards we must attempt to restore to the currently-active device if we can.
                // When switching devices the process follows the order: Init() -> UpdateDevice() -> Free(), so the "currently-active" device may correspond to the device which we've switched to.
                int currentDevice = Bass.CurrentDevice;

                // Free the device.
                Bass.CurrentDevice = deviceIndex;
                Bass.Free();

                // If the previously-current device wasn't the one we just freed, switch back to it.
                if (deviceIndex != currentDevice)
                    Bass.CurrentDevice = currentDevice;
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
            // This will let any final actions execute before the disposal is finalised and the devices can be cleaned up.
            RunFrame();

            base.PerformExit();
        }
    }
}
