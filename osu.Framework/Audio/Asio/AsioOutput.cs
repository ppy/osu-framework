// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ManagedBass;
using ManagedBass.Mix;

namespace osu.Framework.Audio.Asio
{
    /// <summary>
    /// Pulls the game's decoding mixer directly from a native ASIO output callback.
    /// All lifecycle operations run on AudioThread; notifications only set an atomic flag.
    /// </summary>
    internal sealed class AsioOutput : IDisposable
    {
        private bool initialised;
        private int resetRequested;
        private readonly AsioNative.NotifyProcedure notify;
        private int frequency;
        private string deviceName = string.Empty;

        public int MixerHandle { get; private set; }
        public string Status { get; private set; } = string.Empty;

        public AsioOutput()
        {
            // Keep the delegate alive until after BASS_ASIO_Free has joined its threads.
            notify = (_, _) => Interlocked.Exchange(ref resetRequested, 1);
        }

        public static IReadOnlyList<string> GetDeviceNames()
        {
            var names = new List<string>();
            if (!OperatingSystem.IsWindows())
                return names;

            try
            {
                AsioNative.EnsureLoaded();
                for (int i = 0; AsioNative.BASS_ASIO_GetDeviceInfo(i, out var info); i++)
                    names.Add(Marshal.PtrToStringAnsi(info.Name) ?? $"ASIO device {i}");
            }
            catch (Exception exception) when (exception is DllNotFoundException or BadImageFormatException or NotSupportedException or EntryPointNotFoundException)
            {
                // Settings must remain usable even when the native library is unavailable.
            }

            return names;
        }

        public void Initialise(string preferredDevice, int sampleRate, int bufferSize)
        {
            AsioNative.EnsureLoaded();
            int device = -1;

            for (int i = 0; AsioNative.BASS_ASIO_GetDeviceInfo(i, out var info); i++)
            {
                string name = Marshal.PtrToStringAnsi(info.Name) ?? string.Empty;

                if (name == preferredDevice || (string.IsNullOrEmpty(preferredDevice) && device < 0))
                {
                    device = i;
                    deviceName = name;
                    if (!string.IsNullOrEmpty(preferredDevice))
                        break;
                }
            }

            if (device < 0)
                throw new InvalidOperationException(string.IsNullOrEmpty(preferredDevice) ? "No ASIO drivers are installed." : $"Driver '{preferredDevice}' was not found.");

            // BASS_ASIO_THREAD supplies the persistent COM apartment and message pump
            // required by ASIO, independently of the game's threading mode.
            check(AsioNative.BASS_ASIO_Init(device, 1), "open driver");
            initialised = true;
            check(AsioNative.BASS_ASIO_GetInfo(out var driverInfo), "read driver information");
            if (driverInfo.Outputs < 2)
                throw new InvalidOperationException("The selected ASIO driver has fewer than two output channels.");

            if (!IsBufferSizeSupported(bufferSize, driverInfo.MinimumBuffer, driverInfo.MaximumBuffer, driverInfo.BufferGranularity))
                throw new InvalidOperationException($"{bufferSize} samples is unsupported by this driver (range {driverInfo.MinimumBuffer}–{driverInfo.MaximumBuffer}). Choose Driver default or another buffer size.");

            // Some drivers reject SetRate while shared-mode clients are open, even
            // when the hardware is already running at the requested rate.
            if ((int)AsioNative.BASS_ASIO_GetRate() != sampleRate)
                check(AsioNative.BASS_ASIO_SetRate(sampleRate), $"set sample rate to {sampleRate} Hz");
            frequency = checked((int)AsioNative.BASS_ASIO_GetRate());
            if (frequency != sampleRate)
                throw new InvalidOperationException($"The driver selected {frequency} Hz instead of {sampleRate} Hz.");

            MixerHandle = BassMix.CreateMixerStream(frequency, 2, BassFlags.Decode | BassFlags.Float | BassFlags.MixerNonStop | BassFlags.MixerPositionEx);
            if (MixerHandle == 0)
                throw new InvalidOperationException($"Could not create ASIO mixer: {Bass.LastError}");

            // BASSASIO owns the pull callback and joins output 1 into output 0 as interleaved stereo.
            // No WASAPI stream, loopback capture or additional playback buffer is involved.
            check(AsioNative.BASS_ASIO_ChannelEnableBASS(false, 0, MixerHandle, true), "connect stereo output channels 1/2");
            check(AsioNative.BASS_ASIO_SetNotify(notify, IntPtr.Zero), "register driver notifications");
        }

        public void Start(int bufferSize)
        {
            check(AsioNative.BASS_ASIO_Start(bufferSize, 1), "start output");
            check(AsioNative.BASS_ASIO_GetInfo(out var info), "read active buffer size");
            int latencySamples = AsioNative.BASS_ASIO_GetLatency(false);
            if (!Bass.ChannelSetAttribute(MixerHandle, ChannelAttribute.MixerLatency, (float)latencySamples / frequency))
                throw new InvalidOperationException($"Could not set ASIO playback latency: {Bass.LastError}");

            int activeBuffer = bufferSize == 0 ? info.PreferredBuffer : bufferSize;
            Status = $"ASIO active: {deviceName} · {frequency} Hz · {activeBuffer} samples · output latency {1000.0 * latencySamples / frequency:0.00} ms · outputs 1/2";
        }

        public bool ConsumeResetRequest() => Interlocked.Exchange(ref resetRequested, 0) != 0;

        internal static bool IsBufferSizeSupported(int size, int minimum, int maximum, int granularity)
        {
            if (size == 0)
                return true;

            if (size < minimum || size > maximum)
                return false;

            return granularity switch
            {
                -1 => (size & (size - 1)) == 0,
                0 => true,
                _ => (size - minimum) % granularity == 0,
            };
        }

        private static void check(bool success, string operation)
        {
            if (!success)
                throw new InvalidOperationException($"Could not {operation} (ASIO error {AsioNative.BASS_ASIO_ErrorGetCode()}).");
        }

        public void Dispose()
        {
            if (initialised)
            {
                AsioNative.BASS_ASIO_Stop();
                AsioNative.BASS_ASIO_Free();
                initialised = false;
            }

            if (MixerHandle != 0)
            {
                Bass.StreamFree(MixerHandle);
                MixerHandle = 0;
            }

            GC.KeepAlive(notify);
        }
    }

    internal static class AsioNative
    {
        private const string library = "bassasio";

        private static readonly Lazy<IntPtr> loaded = new Lazy<IntPtr>(() =>
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("ASIO output is available on Windows only.");

            string runtime = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86",
                _ => throw new NotSupportedException("The bundled ASIO library supports x64 and x86 Windows."),
            };

            // Load only our bundled binary, rather than searching PATH for arbitrary DLLs.
            string path = Path.Combine(AppContext.BaseDirectory, "runtimes", runtime, "native", "bassasio.dll");
            // NuGet flattens native assets into the application directory for RID-specific publish.
            string publishedPath = Path.Combine(AppContext.BaseDirectory, "bassasio.dll");
            if (File.Exists(publishedPath))
                path = publishedPath;
            return NativeLibrary.Load(path);
        });

        public static void EnsureLoaded()
        {
            _ = loaded.Value;
            if ((BASS_ASIO_GetVersion() >> 16) != 0x104)
                throw new NotSupportedException("BASSASIO 1.4 is required.");
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DeviceInfo
        {
            public IntPtr Name;
            public IntPtr Driver;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct DriverInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string Name;

            public int Version;
            public int Inputs;
            public int Outputs;
            public int MinimumBuffer;
            public int MaximumBuffer;
            public int PreferredBuffer;
            public int BufferGranularity;
            public int InitFlags;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void NotifyProcedure(int notification, IntPtr user);

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int BASS_ASIO_GetVersion();

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int BASS_ASIO_ErrorGetCode();

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BASS_ASIO_GetDeviceInfo(int device, out DeviceInfo info);

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BASS_ASIO_Init(int device, int flags);

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BASS_ASIO_Free();

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BASS_ASIO_GetInfo(out DriverInfo info);

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BASS_ASIO_SetRate(double rate);

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        internal static extern double BASS_ASIO_GetRate();

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BASS_ASIO_ChannelEnableBASS([MarshalAs(UnmanagedType.Bool)] bool input, int channel, int handle, [MarshalAs(UnmanagedType.Bool)] bool join);

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BASS_ASIO_SetNotify(NotifyProcedure procedure, IntPtr user);

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BASS_ASIO_Start(int bufferLength, int threads);

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BASS_ASIO_Stop();

        [DllImport(library, CallingConvention = CallingConvention.StdCall)]
        internal static extern int BASS_ASIO_GetLatency([MarshalAs(UnmanagedType.Bool)] bool input);
    }
}
