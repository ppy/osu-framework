// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using ManagedBass;
using ManagedBass.Asio;
using ManagedBass.Mix;
using ManagedBass.Wasapi;
using osu.Framework.Audio;
using osu.Framework.Audio.Asio;
using osu.Framework.Audio.EzLatency;
using osu.Framework.Audio.Host;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Logging;
using osu.Framework.Platform.Linux.Native;

namespace osu.Framework.Threading
{
    public class AudioThread : GameThread
    {
        private static int wasapiNativeUnavailableLogged;
        private static int asioResolverRegistered;

        internal readonly EzLatencyAnalyzer LatencyAnalyzer;

        public AudioThread()
            : base(name: "Audio")
        {
            // Ensure this AudioThread instance is always reachable from native WASAPI callbacks.
            wasapiUserHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            wasapiUserPtr = GCHandle.ToIntPtr(wasapiUserHandle);

            OnNewFrame += onNewFrame;
            PreloadBass();

            // Initialise EzLatency on the audio thread.
            LatencyAnalyzer = new EzLatencyAnalyzer();

            // Forward any analyzer records from this audio thread into the global EzLatencyService
            LatencyAnalyzer.OnNewRecord += r =>
            {
                EzLatencyService.Instance.PushRecord(r);
            };
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
            StatisticsCounterType.MixChannels,
        };

        private readonly List<AudioManager> managers = new List<AudioManager>();

        private static readonly HashSet<int> initialised_devices = new HashSet<int>();

        private static readonly GlobalStatistic<double> cpu_usage = GlobalStatistics.Get<double>("Audio", "Bass CPU%");

        private long frameCount;

        private void onNewFrame()
        {
            if (frameCount++ % 1000 == 0)
                cpu_usage.Value = Bass.CPUUsage;

            lock (managers)
            {
                for (int i = 0; i < managers.Count; i++)
                {
                    var m = managers[i];
                    m.Update();
                }
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

            // Set the manager reference for event triggering
            Manager ??= manager;

            manager.GlobalMixerHandle.BindTo(globalMixerHandle);
        }

        internal void UnregisterManager(AudioManager manager)
        {
            lock (managers)
                managers.Remove(manager);

            manager.GlobalMixerHandle.UnbindFrom(globalMixerHandle);
        }

        protected override void OnExit()
        {
            base.OnExit();

            lock (managers)
            {
                // AudioManagers are iterated over backwards since disposal will unregister and remove them from the list.
                for (int i = managers.Count - 1; i >= 0; i--)
                {
                    var m = managers[i];

                    m.Dispose();

                    // Audio component disposal (including the AudioManager itself) is scheduled and only runs when the AudioThread updates.
                    // But the AudioThread won't run another update since it's exiting, so an update must be performed manually in order to finish the disposal.
                    m.Update();
                }

                managers.Clear();
            }

            // Safety net to ensure we have freed all devices before exiting.
            // This is mainly required for device-lost scenarios.
            // See https://github.com/ppy/osu-framework/pull/3378 for further discussion.
            foreach (int d in initialised_devices.ToArray())
                FreeDevice(d);

            if (wasapiUserHandle.IsAllocated)
                wasapiUserHandle.Free();
        }

        #region BASS Initialisation

        // TODO: All this bass init stuff should probably not be in this class.

        // WASAPI callbacks must never be allowed to be GC'd while native code may still call into them.
        // Use static delegates with a stable user pointer back to this thread instance.
        private static readonly WasapiProcedure wasapi_procedure_static = (buffer, length, user) =>
        {
            var thread = getWasapiOwner(user);

            int? mixer = thread?.globalMixerHandle.Value;
            if (mixer == null)
                return 0;

            return Bass.ChannelGetData(mixer.Value, buffer, length);
        };

        private static readonly WasapiNotifyProcedure wasapi_notify_procedure_static = (notify, device, user) =>
        {
            var thread = getWasapiOwner(user);
            if (thread == null)
                return;

            if (notify != WasapiNotificationType.DefaultOutput || !thread.shouldTrackDefaultWasapiChanges())
                return;

            thread.Scheduler.Add(() =>
            {
                if (!thread.shouldTrackDefaultWasapiChanges())
                    return;

                thread.freeWasapi();
                thread.initWasapi(device, exclusive: false);
            });
        };

        private static AudioThread? getWasapiOwner(IntPtr user)
        {
            if (user == IntPtr.Zero)
                return null;

            try
            {
                return (AudioThread?)GCHandle.FromIntPtr(user).Target;
            }
            catch
            {
                return null;
            }
        }

        private GCHandle wasapiUserHandle;
        private readonly IntPtr wasapiUserPtr;
        private bool wasapiExclusiveActive;

        private bool shouldTrackDefaultWasapiChanges()
            => !wasapiExclusiveActive && string.IsNullOrEmpty(Manager?.AudioDevice.Value);

        /// <summary>
        /// Reference to the AudioManager that owns this AudioThread.
        /// Used to trigger events when ASIO devices are initialized.
        /// </summary>
        internal AudioManager? Manager { get; set; }

        /// <summary>
        /// If a global mixer is being used, this will be the BASS handle for it.
        /// If non-null, all game mixers should be added to this mixer.
        /// </summary>
        private readonly Bindable<int?> globalMixerHandle = new Bindable<int?>();

        internal bool InitDevice(int deviceId, AudioOutputMode outputMode, double preferredSampleRate, int asioBitDepth = 24)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);
            Trace.Assert(deviceId != -1); // The real device ID should always be used, as the -1 device has special cases which are hard to work with.

            // Important: stop any existing output first.
            // In particular, WASAPI exclusive can hold the device such that a subsequent Bass.Init() returns Busy.
            // If we can't initialise BASS, we also won't get a chance to clean up the previous output mode.
            freeAsio();
            freeWasapi();
            releaseAllOutputsForSwitch(deviceId, outputMode);

            // ASIO: allow the previous driver time to fully release before re-init.
            if (outputMode == AudioOutputMode.Asio && !DebugUtils.IsNUnitRunning)
            {
                Thread.Sleep(100);
            }

            // Try to initialise the device, or request a re-initialise.
            var initFlags = initialised_devices.Contains(deviceId) ? (DeviceInitFlags)16 : 0;

            if (!Bass.Init(deviceId, Flags: initFlags))
            {
                // Treat "Already" as non-fatal: BASS may already be initialised for this device in-process.
                if (Bass.LastError == Errors.Already)
                {
                    Logger.Log($"BASS.Init({deviceId}) returned Already; continuing with existing initialisation.", name: "audio", level: LogLevel.Debug);
                }
                else
                {
                    Logger.Log($"BASS.Init({deviceId}) failed: {Bass.LastError}", name: "audio", level: LogLevel.Error);
                    return false;
                }
            }

            switch (outputMode)
            {
                case AudioOutputMode.Default:
                    break;

                case AudioOutputMode.WasapiShared:
                    if (!attemptWasapiInitialisation(deviceId, exclusive: false))
                    {
                        Logger.Log($"BassWasapi initialisation failed (shared mode). BASS error: {Bass.LastError}", name: "audio", level: LogLevel.Error);
                        return false;
                    }

                    break;

                case AudioOutputMode.WasapiExclusive:
                    if (!attemptWasapiInitialisation(deviceId, exclusive: true))
                    {
                        Logger.Log($"BassWasapi initialisation failed (exclusive mode). BASS error: {Bass.LastError}", name: "audio", level: LogLevel.Error);
                        return false;
                    }

                    break;

                case AudioOutputMode.Asio:
                    // ASIO uses a BassAsio device index, not the BASS playback device id.
                    string selectedDeviceName = Manager?.AudioDevice.Value ?? string.Empty;

                    if (!string.IsNullOrEmpty(selectedDeviceName))
                    {
                        if (EzAsioDeviceManager.TryParseDeviceSelection(selectedDeviceName, out string deviceName))
                        {
                            int? asioDeviceIndex = EzAsioDeviceManager.FindAsioDeviceIndex(deviceName);

                            if (asioDeviceIndex.HasValue)
                            {
                                if (!initAsio(asioDeviceIndex.Value, preferredSampleRate, asioBitDepth, deviceName))
                                    return false;
                            }
                            else
                            {
                                Logger.Log($"Could not find ASIO device: {deviceName}", name: "audio", level: LogLevel.Error);
                                return false;
                            }
                        }
                        else
                        {
                            Logger.Log("Could not parse ASIO device name from selection", name: "audio", level: LogLevel.Error);
                            return false;
                        }
                    }
                    else
                    {
                        Logger.Log("No ASIO device name in selection; falling back to first available device", name: "audio");
                        var availableDevices = EzAsioDeviceManager.EnumerateAsioDevices().ToList();

                        if (availableDevices.Count != 0)
                        {
                            if (!initAsio(availableDevices.First().Index, preferredSampleRate, asioBitDepth, availableDevices.First().Name))
                                return false;
                        }
                        else
                        {
                            Logger.Log("No ASIO devices are available", name: "audio", level: LogLevel.Error);
                            return false;
                        }
                    }

                    break;
            }

            initialised_devices.Add(deviceId);
            return true;
        }

        private static void releaseAllOutputsForSwitch(int targetDeviceId, AudioOutputMode outputMode)
        {
            int currentDevice = Bass.CurrentDevice;

            for (int deviceId = 0; deviceId < Bass.DeviceCount; deviceId++)
            {
                try
                {
                    if (!Bass.GetDeviceInfo(deviceId, out var deviceInfo) || !deviceInfo.IsInitialized)
                        continue;

                    Bass.CurrentDevice = deviceId;
                    Logger.Log($"Releasing BASS output device {deviceId} before switching to {outputMode} (target device {targetDeviceId}).", name: "audio", level: LogLevel.Debug);
                    Bass.Free();
                    initialised_devices.Remove(deviceId);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to release BASS output device {deviceId} before switching to {outputMode}: {ex}", name: "audio", level: LogLevel.Error);
                }
            }

            try
            {
                if (currentDevice >= 0)
                    Bass.CurrentDevice = currentDevice;
            }
            catch
            {
            }

            Thread.Sleep(50);
        }

        internal void FreeDevice(int deviceId)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);

            int selectedDevice = Bass.CurrentDevice;

            // Tear down ASIO before BASS so driver handles are released in order.
            freeAsio();

            if (canSelectDevice(deviceId))
            {
                Bass.CurrentDevice = deviceId;
                Bass.Free();
            }

            freeWasapi();

            if (selectedDevice != deviceId && canSelectDevice(selectedDevice))
                Bass.CurrentDevice = selectedDevice;

            initialised_devices.Remove(deviceId);

            static bool canSelectDevice(int deviceId) => Bass.GetDeviceInfo(deviceId, out var deviceInfo) && deviceInfo.IsInitialized;
        }

        /// <summary>
        /// Makes BASS available to be consumed.
        /// </summary>
        internal static void PreloadBass()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
            {
                if (Interlocked.Exchange(ref asioResolverRegistered, 1) == 1)
                    return;

                NativeLibrary.SetDllImportResolver(typeof(BassAsio).Assembly, resolveBassAsio);
            }

            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
            {
                // required for the time being to address libbass_fx.so load failures (see https://github.com/ppy/osu/issues/2852)
                Library.Load("libbass.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
            }
        }

        private static IntPtr resolveBassAsio(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!libraryName.Equals("bassasio", StringComparison.OrdinalIgnoreCase)
                && !libraryName.Equals("bassasio.dll", StringComparison.OrdinalIgnoreCase))
                return IntPtr.Zero;

            // Try the default resolver first without throwing — a failed ASIO probe must not break non-ASIO paths.
            if (NativeLibrary.TryLoad(libraryName, assembly, searchPath ?? DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.SafeDirectories, out IntPtr result))
                return result;

            // On 64-bit builds, fall back to the x64 native folder.
            if (Environment.Is64BitProcess)
            {
                string dllPath = Path.Combine(AppContext.BaseDirectory, "x64", "bassasio.dll");
                if (File.Exists(dllPath))
                    return NativeLibrary.TryLoad(dllPath, out result) ? result : IntPtr.Zero;
            }
            else
            {
                string dllPath = Path.Combine(AppContext.BaseDirectory, "x86", "bassasio.dll");
                if (File.Exists(dllPath))
                    return NativeLibrary.TryLoad(dllPath, out result) ? result : IntPtr.Zero;
            }

            // Let the runtime use its default resolver if we could not load bassasio.
            return IntPtr.Zero;
        }

        private bool attemptWasapiInitialisation() => attemptWasapiInitialisation(Bass.CurrentDevice, exclusive: false);

        private bool attemptWasapiInitialisation(int bassDeviceId, bool exclusive)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return false;

            Logger.Log("Attempting local BassWasapi initialisation");

            try
            {
                return attemptWasapiInitialisationInternal(bassDeviceId, exclusive);
            }
            catch (DllNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI output is unavailable because basswasapi.dll could not be loaded ({e.Message}).");
                return false;
            }
            catch (EntryPointNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI output is unavailable because basswasapi.dll is incompatible/mismatched ({e.Message}).");
                return false;
            }
            catch (Exception e)
            {
                Logger.Log($"WASAPI initialisation failed with exception: {e}", name: "audio", level: LogLevel.Error);
                return false;
            }
        }

        private bool attemptWasapiInitialisationInternal(int bassDeviceId, bool exclusive)
        {
            Logger.Log($"Attempting local BassWasapi initialisation (exclusive: {exclusive})", name: "audio", level: LogLevel.Verbose);

            int wasapiDevice = -1;

            // WASAPI device indices don't match normal BASS devices.
            // Each device is listed multiple times with each supported channel/frequency pair.
            //
            // Working backwards to find the correct device is how bass does things internally (see BassWasapi.GetBassDevice).
            if (bassDeviceId > 0)
            {
                string driver = Bass.GetDeviceInfo(bassDeviceId).Driver;

                if (!string.IsNullOrEmpty(driver))
                {
                    var candidates = new List<(int index, int freq, int chans)>();

                    // WASAPI device indices don't match normal BASS devices.
                    // Each device is listed multiple times with each supported channel/frequency pair.
                    //
                    // Working backwards to find the correct device is how bass does things internally (see BassWasapi.GetBassDevice).
                    // We replicate this by scanning the full list and remembering the last match.
                    for (int i = 0; i < 16384; i++)
                    {
                        if (!BassWasapi.GetDeviceInfo(i, out WasapiDeviceInfo info))
                            break;

                        // Only consider output devices (not input/loopback), since we're initialising audio output.
                        if (info.ID == driver && info.IsEnabled && !info.IsInput && !info.IsLoopback)
                            candidates.Add((i, info.MixFrequency, info.MixChannels));
                    }

                    if (candidates.Count > 0)
                    {
                        // Prefer common stereo formats.
                        var best = orderWasapiCandidates(candidates).First();

                        wasapiDevice = best.index;
                        Logger.Log($"Mapped BASS device {bassDeviceId} (driver '{driver}') to WASAPI device {wasapiDevice} (mix: {best.freq}Hz/{best.chans}ch).", name: "audio",
                            level: LogLevel.Verbose);

                        // In exclusive mode, the chosen (freq/chans) pair matters. If the preferred candidate fails,
                        // we will retry other candidates below.
                        if (exclusive)
                        {
                            foreach (var candidate in orderWasapiCandidates(candidates))
                            {
                                freeWasapi();

                                if (initWasapi(candidate.index, exclusive))
                                    return true;
                            }

                            Logger.Log($"All WASAPI exclusive format candidates failed for BASS device {bassDeviceId} (driver '{driver}').", name: "audio", level: LogLevel.Verbose);
                            return false;
                        }
                    }
                    else
                    {
                        // If the user selected a specific non-default device, do not fall back to system default.
                        // Fallback would likely be busy (e.g. browser playing on default), and would mask the real issue.
                        Logger.Log($"Could not map BASS device {bassDeviceId} (driver '{driver}') to a WASAPI output device; refusing to fall back to default (-1).", name: "audio",
                            level: LogLevel.Verbose);
                        return false;
                    }
                }
                else
                {
                    Logger.Log($"BASS device {bassDeviceId} did not provide a driver identifier; falling back to default WASAPI device (-1).", name: "audio", level: LogLevel.Verbose);
                }
            }

            if (wasapiDevice == -1)
                Logger.Log("Using default WASAPI device (-1).", name: "audio", level: LogLevel.Verbose);

            // To keep things in a sane state let's only keep one device initialised via wasapi.
            freeWasapi();
            return initWasapi(wasapiDevice, exclusive);
        }

        private bool initWasapi(int wasapiDevice, bool exclusive)
        {
            try
            {
                wasapiExclusiveActive = exclusive;

                // BASSWASAPI flags:
                // - 0x1  = EXCLUSIVE
                // - 0x10 = EVENT (event-driven)
                // ManagedBass bindings used here do not currently expose Exclusive, so we use the documented value.
                const WasapiInitFlags exclusive_flag = (WasapiInitFlags)0x1;

                int requestedFrequency = 0;
                int requestedChannels = 0;

                // Shared mode can use event-driven callbacks and auto-format.
                // Exclusive mode should use an explicit supported format (freq/chans) from the chosen WASAPI entry.
                var flags = (WasapiInitFlags)0;

                if (exclusive)
                {
                    flags |= exclusive_flag;

                    if (wasapiDevice >= 0 && BassWasapi.GetDeviceInfo(wasapiDevice, out WasapiDeviceInfo selectedInfo))
                    {
                        requestedFrequency = selectedInfo.MixFrequency;
                        requestedChannels = selectedInfo.MixChannels;

                        int currentBassDevice = Bass.CurrentDevice;

                        if ((requestedFrequency <= 0 || requestedChannels <= 0) && currentBassDevice > 0)
                        {
                            string driver = Bass.GetDeviceInfo(currentBassDevice).Driver;
                            string deviceName = Bass.GetDeviceInfo(currentBassDevice).Name;
                            var mixFormat = HostAudioFormatQuery.TryGetMixFormat(driver, deviceName);

                            if (mixFormat != null)
                            {
                                requestedFrequency = mixFormat.Value.sampleRate;
                                requestedChannels = mixFormat.Value.channels;
                            }
                        }
                    }
                }
                else
                {
                    flags |= WasapiInitFlags.EventDriven | WasapiInitFlags.AutoFormat;
                }

                // Important: in exclusive mode, the underlying implementation may not support event-driven callbacks
                // and can fall back to polling. Using a near-zero period (float.Epsilon) can then cause a busy-loop,
                // leading to time running far too fast (and eventual instability elsewhere).
                float bufferSeconds = exclusive ? AudioOutputDefaults.WASAPI_EXCLUSIVE_BUFFER_SECONDS : 0f;
                float periodSeconds = exclusive ? AudioOutputDefaults.WASAPI_EXCLUSIVE_PERIOD_SECONDS : float.Epsilon;

                bool initialised = BassWasapi.Init(wasapiDevice, Frequency: requestedFrequency, Channels: requestedChannels, Procedure: wasapi_procedure_static, Flags: flags, Buffer: bufferSeconds,
                    Period: periodSeconds, User: wasapiUserPtr);
                Logger.Log(
                    $"Initialising BassWasapi for device {wasapiDevice} (exclusive: {exclusive}, buffer: {bufferSeconds:0.###}s, period: {periodSeconds:0.###}s)...{(initialised ? "success!" : "FAILED")}",
                    name: "audio", level: LogLevel.Verbose);

                if (!initialised)
                    return false;

                BassWasapi.GetInfo(out var wasapiInfo);
                Logger.Log($"WASAPI info: Freq={wasapiInfo.Frequency}, Chans={wasapiInfo.Channels}, Format={wasapiInfo.Format}", name: "audio", level: LogLevel.Verbose);
                globalMixerHandle.Value = BassMix.CreateMixerStream(wasapiInfo.Frequency, wasapiInfo.Channels, BassFlags.MixerNonStop | BassFlags.Decode | BassFlags.Float);

                if (globalMixerHandle.Value == 0)
                {
                    Logger.Log($"Failed to create WASAPI mixer stream for device {wasapiDevice}: {Bass.LastError}", name: "audio", level: LogLevel.Error);
                    freeWasapi();
                    return false;
                }

                BassWasapi.SetNotify(shouldTrackDefaultWasapiChanges() ? wasapi_notify_procedure_static : null, shouldTrackDefaultWasapiChanges() ? wasapiUserPtr : IntPtr.Zero);

                if (!BassWasapi.Start())
                {
                    Logger.Log($"BassWasapi.Start() failed for device {wasapiDevice}: {Bass.LastError}", name: "audio", level: LogLevel.Error);
                    freeWasapi();
                    return false;
                }

                return true;
            }
            catch (DllNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI output is unavailable because basswasapi.dll could not be loaded ({e.Message}).");
                freeWasapi();
                return false;
            }
            catch (EntryPointNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI output is unavailable because basswasapi.dll is incompatible/mismatched ({e.Message}).");
                freeWasapi();
                return false;
            }
            catch (Exception e)
            {
                Logger.Log($"WASAPI init failed with exception: {e}", name: "audio", level: LogLevel.Error);
                freeWasapi();
                return false;
            }
        }

        private void freeWasapi()
        {
            int? mixerToFree = globalMixerHandle.Value;

            try
            {
                // The mixer probably doesn't need to be recycled. Just keeping things sane for now.
                // Stop WASAPI first to prevent callbacks from accessing disposed resources.
                BassWasapi.SetNotify(null, IntPtr.Zero);
                BassWasapi.Stop();
                BassWasapi.Free();

                if (mixerToFree != null)
                    Bass.StreamFree(mixerToFree.Value);
            }
            catch (DllNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI cleanup failed because basswasapi.dll could not be loaded ({e.Message}).");
            }
            catch (EntryPointNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI cleanup failed because basswasapi.dll is incompatible/mismatched ({e.Message}).");
            }
            catch (Exception e)
            {
                Logger.Log($"WASAPI cleanup failed with exception: {e}", name: "audio", level: LogLevel.Error);
            }
            finally
            {
                wasapiExclusiveActive = false;
                globalMixerHandle.Value = null;
                Thread.Sleep(50);
            }
        }

        private static void logWasapiNativeUnavailableOnce(string message)
        {
            if (Interlocked.Exchange(ref wasapiNativeUnavailableLogged, 1) == 1)
                return;

            Logger.Log(message, name: "audio", level: LogLevel.Error);
        }

        /// <summary>
        /// Reconfigures the active ASIO device without releasing all BASS output devices.
        /// </summary>
        internal bool TryReconfigureAsio(int asioDeviceIndex, double preferredSampleRate, int bitDepth, int bufferSize)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);

            if (Manager == null)
                return false;

            if (EzAsioDeviceManager.ActiveDeviceIndex != asioDeviceIndex)
                return false;

            preferredSampleRate = EzAsioDeviceManager.GetTargetSampleRate(Manager.SampleRate.Value == 0 ? preferredSampleRate : Manager.SampleRate.Value);
            bitDepth = EzAsioDeviceManager.GetTargetBitDepth(bitDepth);
            bufferSize = EzAsioDeviceManager.GetTargetBufferSize(bufferSize);
            bool nativePassThrough = Manager.AsioPassThrough.Value;

            releaseAsioMixerOnly();

            bool formatChangeRequired = !nativePassThrough && requiresAsioFormatChange(preferredSampleRate, bitDepth);

            if (!formatChangeRequired)
            {
                if (!EzAsioDeviceManager.TryRestartWithBuffer(bufferSize))
                    return false;

                return completeAsioMixerAndStart(bufferSize);
            }

            if (!EzAsioDeviceManager.ReconfigureDevice(asioDeviceIndex, preferredSampleRate, bitDepth, bufferSize, nativePassThrough))
                return false;

            return completeAsioMixerAndStart(bufferSize);
        }

        private static bool requiresAsioFormatChange(double preferredSampleRate, int bitDepth)
        {
            if (EzAsioDeviceManager.GetCurrentDeviceInfo() == null)
                return true;

            double currentRate = EzAsioDeviceManager.GetCurrentSampleRate();

            if (currentRate <= 0 || Math.Abs(currentRate - preferredSampleRate) >= 1.0)
                return true;

            return EzAsioDeviceManager.TargetBitDepth != bitDepth;
        }

        private bool initAsio(int asioDeviceIndex, double preferredSampleRate, int bitDepth, string asioDeviceName)
        {
            freeAsio();

            if (Manager == null)
                return false;

            preferredSampleRate = EzAsioDeviceManager.GetTargetSampleRate(Manager.SampleRate.Value == 0 ? preferredSampleRate : Manager.SampleRate.Value);
            bitDepth = EzAsioDeviceManager.GetTargetBitDepth(bitDepth);
            int bufferSize = Manager.AsioBufferSize.Value;

            if (EzAsioDeviceManager.IsDeviceRunning())
            {
                Logger.Log("ASIO device already running, stopping before initialization", name: "audio", level: LogLevel.Debug);
                EzAsioDeviceManager.StopDevice();
                Thread.Sleep(100);
            }

            if (EzAsioDeviceManager.RequiresVirtualHostWarmUp(asioDeviceName))
                warmUpHostAudioForVirtualAsio(asioDeviceName);

            if (!ensureBassOutputDeviceReady(Bass.CurrentDevice))
            {
                Logger.Log($"BASS output device {Bass.CurrentDevice} is not initialised after ASIO warm-up; cannot create mixer.", name: "audio", level: LogLevel.Error);
                return false;
            }

            bool nativePassThrough = Manager.AsioPassThrough.Value;

            if (!EzAsioDeviceManager.InitializeDevice(asioDeviceIndex, preferredSampleRate, bufferSize, bitDepth, nativePassThrough, waitForDevice: true, waitTimeoutMs: 10_000))
            {
                Logger.Log($"EzAsioDeviceManager.InitializeDevice({asioDeviceIndex}, {preferredSampleRate}, {bufferSize}, {bitDepth}, native={nativePassThrough}) failed", name: "audio", level: LogLevel.Error);
                return false;
            }

            return completeAsioMixerAndStart(bufferSize);
        }

        private bool completeAsioMixerAndStart(int bufferSize)
        {
            if (Manager == null)
                return false;

            var deviceInfo = EzAsioDeviceManager.GetCurrentDeviceInfo();

            if (deviceInfo == null)
            {
                Logger.Log("Unable to get ASIO device info after initialisation", name: "audio", level: LogLevel.Error);
                freeAsio();
                return false;
            }

            int outputChannels = Math.Max(1, deviceInfo.Value.Outputs);

            if (outputChannels < 2)
            {
                Logger.Log($"ASIO device has insufficient output channels ({outputChannels}); stereo requires at least 2", name: "audio", level: LogLevel.Important);
                freeAsio();
                FreeDevice(Bass.CurrentDevice);
                return false;
            }

            double sampleRate = BassAsio.Rate;

            if (sampleRate <= 0 || sampleRate > 1000000 || double.IsNaN(sampleRate) || double.IsInfinity(sampleRate))
            {
                Logger.Log($"Invalid ASIO sample rate ({sampleRate}Hz); cannot start output.", name: "audio", level: LogLevel.Error);
                freeAsio();
                return false;
            }

            const int mixer_channels = 2;
            globalMixerHandle.Value = BassMix.CreateMixerStream((int)sampleRate, mixer_channels, BassFlags.MixerNonStop | BassFlags.Decode | BassFlags.Float);

            if (globalMixerHandle.Value == 0)
            {
                var mixerError = Bass.LastError;
                Logger.Log($"Failed to create ASIO mixer stream: {(int)mixerError} ({mixerError}), sampleRate={sampleRate}", name: "audio", level: LogLevel.Error);
                freeAsio();
                return false;
            }

            // Decode mixers cannot be started with ChannelPlay (BASS returns Decode). ASIO output pulls from the mixer via BassAsio, same as WASAPI.
            EzAsioDeviceManager.SetGlobalMixerHandle(globalMixerHandle.Value.Value);

            if (!EzAsioDeviceManager.StartDevice(bufferSize))
            {
                Logger.Log("EzAsioDeviceManager.StartDevice() failed", name: "audio", level: LogLevel.Error);
                freeAsio();
                return false;
            }

            int activeBufferSize = EzAsioDeviceManager.ActiveBufferSize > 0 ? EzAsioDeviceManager.ActiveBufferSize : bufferSize;
            int activeBitDepth = EzAsioDeviceManager.TargetBitDepth;

            Manager.OnAsioDeviceConfigurationChanged?.Invoke(sampleRate, activeBufferSize, activeBitDepth);
            Manager.OnAsioDeviceInitialized?.Invoke(sampleRate);
            return true;
        }

        private void releaseAsioMixerOnly()
        {
            if (globalMixerHandle.Value != null)
            {
                try
                {
                    Bass.StreamFree(globalMixerHandle.Value.Value);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception freeing ASIO mixer: {ex.Message}", name: "audio", level: LogLevel.Error);
                }

                globalMixerHandle.Value = null;
            }

            EzAsioDeviceManager.StopDevice();
        }

        private void warmUpHostAudioForVirtualAsio(string asioDeviceName)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return;

            int targetDevice = findMatchingBassDeviceId(asioDeviceName) ?? Bass.CurrentDevice;

            if (targetDevice < 0)
                return;

            int previousDevice = Bass.CurrentDevice;

            try
            {
                // Waking the matching host endpoint can help virtual ASIO drivers (ASIO4ALL, Voicemeeter, etc.).
                // Never Bass.Free() the device we are already using for mixer creation — that leaves BASS uninitialised and produces silent output.
                if (targetDevice == previousDevice)
                {
                    Logger.Log($"Warming up active BASS device {targetDevice} before ASIO initialisation", name: "audio", level: LogLevel.Debug);

                    if (attemptWasapiInitialisation(targetDevice, exclusive: false))
                        Thread.Sleep(50);

                    freeWasapi();
                    return;
                }

                Logger.Log($"Warming up host BASS device {targetDevice} (active device {previousDevice}) before ASIO initialisation", name: "audio", level: LogLevel.Debug);

                Bass.CurrentDevice = targetDevice;

                if (!Bass.Init(targetDevice) && Bass.LastError != Errors.Already)
                    return;

                if (attemptWasapiInitialisation(targetDevice, exclusive: false))
                    Thread.Sleep(50);

                freeWasapi();
                Bass.Free();
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                Logger.Log($"Host audio warm-up before ASIO failed: {ex.Message}", name: "audio", level: LogLevel.Debug);
            }
            finally
            {
                ensureBassOutputDeviceReady(previousDevice);
            }
        }

        private static int? findMatchingBassDeviceId(string asioDeviceName)
        {
            string normalisedAsio = normaliseDeviceNameForMatch(asioDeviceName);

            for (int deviceId = 2; deviceId < Bass.DeviceCount; deviceId++)
            {
                if (!Bass.GetDeviceInfo(deviceId, out DeviceInfo info) || !info.IsEnabled)
                    continue;

                if (deviceNamesMatch(normalisedAsio, normaliseDeviceNameForMatch(info.Name)))
                    return deviceId;
            }

            return null;
        }

        private static bool ensureBassOutputDeviceReady(int deviceId)
        {
            if (deviceId < 0)
                return false;

            try
            {
                Bass.CurrentDevice = deviceId;

                if (Bass.GetDeviceInfo(deviceId, out DeviceInfo info) && info.IsInitialized)
                    return true;

                if (Bass.Init(deviceId))
                    return true;

                if (Bass.LastError == Errors.Already && Bass.GetDeviceInfo(deviceId, out info))
                    return info.IsInitialized;

                Logger.Log($"Failed to ensure BASS device {deviceId} is initialised: {Bass.LastError}", name: "audio", level: LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception ensuring BASS device {deviceId}: {ex}", name: "audio", level: LogLevel.Error);
                return false;
            }
        }

        private static string normaliseDeviceNameForMatch(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            return name.Replace("(ASIO)", string.Empty, StringComparison.OrdinalIgnoreCase)
                       .Replace("(WASAPI Exclusive)", string.Empty, StringComparison.OrdinalIgnoreCase)
                       .Trim();
        }

        private static bool deviceNamesMatch(string asioName, string bassName)
        {
            if (string.IsNullOrEmpty(asioName) || string.IsNullOrEmpty(bassName))
                return false;

            if (asioName.Contains(bassName, StringComparison.OrdinalIgnoreCase) || bassName.Contains(asioName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Virtual ASIO drivers often share a prefix with the underlying device (Voicemeeter, ASIO4ALL, etc.).
            static string firstToken(string value) => value.Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? value;

            return string.Equals(firstToken(asioName), firstToken(bassName), StringComparison.OrdinalIgnoreCase);
        }

        private static IOrderedEnumerable<(int index, int freq, int chans)> orderWasapiCandidates(IEnumerable<(int index, int freq, int chans)> candidates)
            => candidates.OrderBy(c => c.chans == 2 ? 0 : 1)
                         .ThenBy(c => Math.Abs(c.freq - AudioOutputDefaults.DEFAULT_SAMPLE_RATE))
                         .ThenBy(c => Math.Abs(c.freq - AudioOutputDefaults.SECONDARY_SAMPLE_RATE));

        private void freeAsio()
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return;

            Logger.Log("Freeing ASIO device", name: "audio", level: LogLevel.Important);

            try
            {
                EzAsioDeviceManager.StopDevice();

                Thread.Sleep(50);

                EzAsioDeviceManager.FreeDevice();
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception while cleaning up ASIO device: {ex.Message}; attempting force reset", name: "audio", level: LogLevel.Error);

                try
                {
                    EzAsioDeviceManager.ForceReset();
                }
                catch (Exception resetEx)
                {
                    Logger.Log($"Force reset also failed: {resetEx.Message}", name: "audio", level: LogLevel.Error);
                }
            }

            if (globalMixerHandle.Value != null)
            {
                try
                {
                    Bass.StreamFree(globalMixerHandle.Value.Value);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception while freeing ASIO mixer: {ex.Message}", name: "audio", level: LogLevel.Error);
                }

                globalMixerHandle.Value = null;
            }

            // Extra delay after ASIO teardown to avoid busy-driver errors when switching devices.
            if (!DebugUtils.IsNUnitRunning)
                Thread.Sleep(300);
        }

        #endregion
    }
}
