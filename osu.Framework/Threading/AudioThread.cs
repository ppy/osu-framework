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

            // 初始化EzLatency模块
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

            thread.Scheduler.Add(() =>
            {
                if (notify == WasapiNotificationType.DefaultOutput)
                {
                    thread.freeWasapi();
                    thread.initWasapi(device, thread.wasapiExclusiveActive);
                }
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

        internal bool InitDevice(int deviceId, AudioOutputMode outputMode, double preferredSampleRate)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);
            Trace.Assert(deviceId != -1); // The real device ID should always be used, as the -1 device has special cases which are hard to work with.

            // Important: stop any existing output first.
            // In particular, WASAPI exclusive can hold the device such that a subsequent Bass.Init() returns Busy.
            // If we can't initialise BASS, we also won't get a chance to clean up the previous output mode.
            freeAsio();
            freeWasapi();

            // 对于ASIO模式，在初始化前添加额外延迟以确保设备完全释放
            if (outputMode == AudioOutputMode.Asio)
            {
                // 增加延迟以确保设备完全释放
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
                    // 对于ASIO模式，我们需要获取实际的ASIO设备索引，而不是使用BASS设备ID
                    // 从AudioManager获取当前选择的设备名称
                    string selectedDeviceName = Manager?.AudioDevice.Value ?? string.Empty;

                    if (!string.IsNullOrEmpty(selectedDeviceName))
                    {
                        // 解析选择以获取设备名称
                        var (mode, deviceName) = parseAudioSelection(selectedDeviceName);

                        if (!string.IsNullOrEmpty(deviceName))
                        {
                            int? asioDeviceIndex = AsioDeviceManager.FindAsioDeviceIndex(deviceName);

                            if (asioDeviceIndex.HasValue)
                            {
                                if (!initAsio(asioDeviceIndex.Value, preferredSampleRate))
                                    return false;
                            }
                            else
                            {
                                Logger.Log($"无法找到ASIO设备: {deviceName}, Mode: {mode}", name: "audio", level: LogLevel.Error);
                                return false;
                            }
                        }
                        else
                        {
                            Logger.Log("无法从选择中提取ASIO设备名称", name: "audio", level: LogLevel.Error);
                            return false;
                        }
                    }
                    else
                    {
                        Logger.Log("没有选择ASIO设备名称", name: "audio");
                        // 默认使用第一个可用的ASIO设备
                        var availableDevices = AsioDeviceManager.EnumerateAsioDevices().ToList();

                        if (availableDevices.Count != 0)
                        {
                            if (!initAsio(availableDevices.First().Index, preferredSampleRate))
                                return false;
                        }
                        else
                        {
                            Logger.Log("没有可用的ASIO设备", name: "audio", level: LogLevel.Error);
                            return false;
                        }
                    }

                    break;
            }

            initialised_devices.Add(deviceId);
            return true;
        }

        internal void FreeDevice(int deviceId)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);

            int selectedDevice = Bass.CurrentDevice;

            // 对于ASIO设备，先释放ASIO再释放BASS以确保正确的清理顺序
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

            // 从架构特定的目录加载bassasio.dll
            IntPtr result = NativeLibrary.Load(libraryName, assembly, searchPath ?? DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.SafeDirectories);
            if (result != IntPtr.Zero)
                return result;

            // 如果是64位进程，尝试从x64目录加载
            if (Environment.Is64BitProcess)
            {
                string dllPath = Path.Combine(AppContext.BaseDirectory, "x64", "bassasio.dll");
                if (File.Exists(dllPath))
                    return NativeLibrary.Load(dllPath);
            }
            else
            {
                string dllPath = Path.Combine(AppContext.BaseDirectory, "x86", "bassasio.dll");
                if (File.Exists(dllPath))
                    return NativeLibrary.Load(dllPath);
            }

            // 如果所有尝试都失败，让运行时使用默认解析
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
                        var best = candidates
                                   .OrderBy(c => c.chans == 2 ? 0 : 1)
                                   .ThenBy(c => Math.Abs(c.freq - 48000))
                                   .ThenBy(c => Math.Abs(c.freq - 44100))
                                   .First();

                        wasapiDevice = best.index;
                        Logger.Log($"Mapped BASS device {bassDeviceId} (driver '{driver}') to WASAPI device {wasapiDevice} (mix: {best.freq}Hz/{best.chans}ch).", name: "audio",
                            level: LogLevel.Verbose);

                        // In exclusive mode, the chosen (freq/chans) pair matters. If the preferred candidate fails,
                        // we will retry other candidates below.
                        if (exclusive)
                        {
                            foreach (var candidate in candidates
                                                      .OrderBy(c => c.chans == 2 ? 0 : 1)
                                                      .ThenBy(c => Math.Abs(c.freq - 48000))
                                                      .ThenBy(c => Math.Abs(c.freq - 44100)))
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
                    }
                }
                else
                {
                    flags |= WasapiInitFlags.EventDriven | WasapiInitFlags.AutoFormat;
                }

                // Important: in exclusive mode, the underlying implementation may not support event-driven callbacks
                // and can fall back to polling. Using a near-zero period (float.Epsilon) can then cause a busy-loop,
                // leading to time running far too fast (and eventual instability elsewhere).
                float bufferSeconds = exclusive ? 0.05f : 0f;
                float periodSeconds = exclusive ? 0.01f : float.Epsilon;

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
                BassWasapi.Start();

                BassWasapi.SetNotify(wasapi_notify_procedure_static, wasapiUserPtr);
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
                globalMixerHandle.Value = null;
            }
        }

        private static void logWasapiNativeUnavailableOnce(string message)
        {
            if (Interlocked.Exchange(ref wasapiNativeUnavailableLogged, 1) == 1)
                return;

            Logger.Log(message, name: "audio", level: LogLevel.Error);
        }

        private bool initAsio(int asioDeviceIndex, double preferredSampleRate = 48000)
        {
            // 首先确保之前的ASIO设备完全释放
            freeAsio();

            // 使用来自AudioManager的统一采样率和缓冲区大小
            if (Manager != null)
            {
                preferredSampleRate = Manager.SampleRate.Value == 0 ? preferredSampleRate : Manager.SampleRate.Value;
                int bufferSize = Manager.AsioBufferSize.Value;

                // 验证当前设备是否已在运行，如果是则先停止
                if (AsioDeviceManager.IsDeviceRunning())
                {
                    Logger.Log("ASIO device already running, stopping before initialization", name: "audio", level: LogLevel.Debug);
                    AsioDeviceManager.StopDevice();
                    Thread.Sleep(100);
                }

                if (!AsioDeviceManager.InitializeDevice(asioDeviceIndex, preferredSampleRate, bufferSize))
                {
                    Logger.Log($"AsioDeviceManager.InitializeDevice({asioDeviceIndex}, {preferredSampleRate}, {bufferSize}) 失败", name: "audio", level: LogLevel.Error);
                    // 不要自动释放BASS设备 - 让AudioManager处理回退决策
                    // 这可以防止过度激进的设备切换导致设备可用性降低
                    return false;
                }

                // 初始化后获取设备信息
                var deviceInfo = AsioDeviceManager.GetCurrentDeviceInfo();

                if (deviceInfo == null)
                {
                    Logger.Log("初始化后无法获取ASIO设备信息", name: "audio", level: LogLevel.Error);
                    freeAsio();
                    // 不要自动释放BASS设备 - 让AudioManager处理回退决策
                    return false;
                }

                int outputChannels = Math.Max(1, deviceInfo.Value.Outputs);
                // int inputChannels = Math.Max(0, deviceInfo.Value.Inputs);

                // 验证设备信息
                if (outputChannels < 2)
                {
                    Logger.Log($"ASIO设备输出通道不足 ({outputChannels})，立体声至少需要2个通道", name: "audio", level: LogLevel.Important);
                    freeAsio();
                    FreeDevice(Bass.CurrentDevice);
                    return false;
                }

                double sampleRate = BassAsio.Rate;
                Logger.Log($"ASIO设备采样率: {sampleRate}Hz", name: "audio", level: LogLevel.Verbose);

                // 验证采样率
                if (sampleRate <= 0 || sampleRate > 1000000 || double.IsNaN(sampleRate) || double.IsInfinity(sampleRate))
                {
                    Logger.Log($"检测到无效采样率 ({sampleRate}Hz)，使用 {AsioDeviceManager.DEFAULT_SAMPLE_RATE}Hz 作为回退", name: "audio", level: LogLevel.Important);
                    sampleRate = AsioDeviceManager.DEFAULT_SAMPLE_RATE;
                }

                // Logger.Log($"使用ASIO设备配置 - 输出: {outputChannels}, 输入: {inputChannels}, 采样率: {sampleRate}Hz", name: "audio", level: LogLevel.Verbose);

                // 创建立体声混音器（游戏音频始终是立体声）
                const int mixer_channels = 2;
                // Logger.Log($"创建ASIO混音器流: 采样率={sampleRate}, 混音器通道={mixer_channels} (立体声)", name: "audio", level: LogLevel.Verbose);
                globalMixerHandle.Value = BassMix.CreateMixerStream((int)sampleRate, mixer_channels, BassFlags.MixerNonStop | BassFlags.Decode | BassFlags.Float);

                if (globalMixerHandle.Value == 0)
                {
                    var mixerError = Bass.LastError;
                    Logger.Log($"创建ASIO混音器流失败: {(int)mixerError} ({mixerError}), 采样率={sampleRate}, 通道={mixer_channels}", name: "audio", level: LogLevel.Error);
                    freeAsio();
                    // 释放BASS设备以便AudioManager可以使用不同的设备重试
                    FreeDevice(Bass.CurrentDevice);
                    return false;
                }

                // Logger.Log($"创建了带有 {mixer_channels} 个通道的ASIO混音器流，采样率为 {sampleRate}Hz (句柄: {globalMixerHandle.Value})", name: "audio", level: LogLevel.Verbose);

                // 为ASIO设备管理器设置全局混音器句柄
                AsioDeviceManager.SetGlobalMixerHandle(globalMixerHandle.Value.Value);

                // 使用设备管理器启动ASIO设备
                if (!AsioDeviceManager.StartDevice())
                {
                    Logger.Log("AsioDeviceManager.StartDevice() 失败", name: "audio", level: LogLevel.Error);
                    freeAsio();
                    // 不要自动释放BASS设备 - 让AudioManager处理回退决策
                    return false;
                }

                // Logger.Log($"ASIO设备初始化成功 - 采样率: {sampleRate}Hz, 输出: {outputChannels}, 输入: {inputChannels}", name: "audio", level: LogLevel.Important);

                // 通知ASIO设备已使用实际采样率初始化
                Manager?.OnAsioDeviceInitialized?.Invoke(sampleRate);
            }

            return true;
        }

        private void freeAsio()
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return;

            Logger.Log("Freeing ASIO device", name: "audio", level: LogLevel.Important);

            try
            {
                // 首先停止ASIO设备
                AsioDeviceManager.StopDevice();

                // 停止后稍作延迟让设备稳定
                Thread.Sleep(50);

                // 释放ASIO设备
                AsioDeviceManager.FreeDevice();
            }
            catch (Exception ex)
            {
                Logger.Log($"ASIO设备清理过程中出现异常: {ex.Message}，尝试强制重置", name: "audio", level: LogLevel.Error);

                try
                {
                    AsioDeviceManager.ForceReset();
                }
                catch (Exception resetEx)
                {
                    Logger.Log($"强制重置也失败: {resetEx.Message}", name: "audio", level: LogLevel.Error);
                }
            }

            // 清理混音器句柄
            if (globalMixerHandle.Value != null)
            {
                try
                {
                    Bass.StreamFree(globalMixerHandle.Value.Value);
                }
                catch (Exception ex)
                {
                    Logger.Log($"释放ASIO混音器时出现异常: {ex.Message}", name: "audio", level: LogLevel.Error);
                }

                globalMixerHandle.Value = null;
            }

            // 释放ASIO设备后添加较长延迟
            // 这可以防止设备繁忙错误，当在ASIO设备之间切换时
            Thread.Sleep(300);
        }

        /// <summary>
        /// 获取ASIO驱动的输出延迟
        /// </summary>
        /// <returns>输出延迟（毫秒），失败返回-1</returns>
        internal double GetAsioOutputLatency()
        {
            try
            {
                if (BassAsio.IsStarted)
                {
                    // 估算 ASIO 输出延迟
                    return 2.0; // ASIO 通常延迟较低
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"获取ASIO输出延迟失败: {ex.Message}", name: "audio", level: LogLevel.Error);
            }

            return -1;
        }

        /// <summary>
        /// 获取WASAPI流的延迟
        /// </summary>
        /// <returns>流延迟（毫秒），失败返回-1</returns>
        internal double GetWasapiStreamLatency()
        {
            try
            {
                if (BassWasapi.IsStarted)
                {
                    // 使用 IAudioClient::GetStreamLatency
                    // 注意：BassWasapi 可能不直接暴露这个，需要通过底层 API
                    // 暂时返回估算值
                    return 10.0; // 估算的 WASAPI 延迟
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"获取WASAPI流延迟失败: {ex.Message}", name: "audio", level: LogLevel.Error);
            }

            return -1;
        }

        /// <summary>
        /// 获取当前音频驱动的缓冲区延迟
        /// </summary>
        /// <param name="outputMode">输出模式</param>
        /// <returns>缓冲区延迟（毫秒），失败返回-1</returns>
        internal double GetDriverBufferLatency(AudioOutputMode outputMode)
        {
            switch (outputMode)
            {
                case AudioOutputMode.Asio:
                    return GetAsioOutputLatency();

                case AudioOutputMode.WasapiExclusive:
                case AudioOutputMode.WasapiShared:
                    return GetWasapiStreamLatency();

                default:
                    return -1;
            }
        }

        #endregion

        /// <summary>
        /// 解析音频设备选择字符串以获取设备名称
        /// </summary>
        /// <param name="selection">选择字符串</param>
        /// <returns>输出模式和设备名称的元组</returns>
        private (AudioOutputMode mode, string deviceName) parseAudioSelection(string selection)
        {
            const string type_asio = "ASIO";

            // 默认设备
            if (string.IsNullOrEmpty(selection))
            {
                return (AudioOutputMode.Default, string.Empty);
            }

            // 检查是否为ASIO设备
            const string suffix = $" ({type_asio})";

            if (selection.EndsWith(suffix, StringComparison.Ordinal))
            {
                string deviceName = selection[..^suffix.Length];
                return (AudioOutputMode.Asio, deviceName);
            }

            // 其他情况返回默认值
            return (AudioOutputMode.Default, selection);
        }
    }
}
