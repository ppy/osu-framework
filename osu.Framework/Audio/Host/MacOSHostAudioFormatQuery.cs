// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Audio.Asio;
using osu.Framework.Logging;

namespace osu.Framework.Audio.Host
{
    /// <summary>
    /// Read-only Core Audio queries for device-reported stream formats on macOS.
    /// </summary>
    internal static class MacOSHostAudioFormatQuery
    {
        private const uint k_audio_object_system_object = 1;
        private const uint k_audio_object_property_scope_global = 0x6C626C67; // 'glob'
        private const uint k_audio_object_property_scope_output = 0x7074756F; // 'outp'
        private const uint k_audio_object_property_element_main = 0;
        private const uint k_audio_hardware_property_default_output_device = 0x74756F64; // 'dOut'
        private const uint k_audio_hardware_property_devices = 0x23646576; // 'dev#'
        private const uint k_audio_device_property_stream_format = 0x73666D74; // 'sfmt'
        private const uint k_audio_object_property_name = 0x6C6E616D; // 'lnam'
        private const uint k_audio_format_linear_pcm = 0x6C70636D; // 'lpcm'
        private const uint k_audio_format_flag_is_float = 1u << 0;

        public static IReadOnlyList<EzAsioFormatOption> GetReportedPlaybackFormats(string? bassDeviceName)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.macOS)
                return Array.Empty<EzAsioFormatOption>();

            try
            {
                uint deviceId = findOutputDeviceId(bassDeviceName) ?? getDefaultOutputDeviceId();

                if (deviceId == 0 || !tryGetStreamFormat(deviceId, out AudioStreamBasicDescription asbd))
                    return Array.Empty<EzAsioFormatOption>();

                int bits = bitsFromAsbd(asbd);

                if (bits == 0)
                    return Array.Empty<EzAsioFormatOption>();

                return new[] { new EzAsioFormatOption((int)Math.Round(asbd.mSampleRate), bits) };
            }
            catch (Exception ex)
            {
                Logger.Log($"macOS audio format lookup failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
                return Array.Empty<EzAsioFormatOption>();
            }
        }

        public static (int sampleRate, int bitDepth)? TryGetDefaultPlaybackFormat()
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.macOS)
                return null;

            uint deviceId = getDefaultOutputDeviceId();

            if (deviceId == 0 || !tryGetStreamFormat(deviceId, out AudioStreamBasicDescription asbd))
                return null;

            int bits = bitsFromAsbd(asbd);

            if (bits == 0)
                return null;

            return ((int)Math.Round(asbd.mSampleRate), bits);
        }

        public static (int sampleRate, int channels)? TryGetMixFormat(string? bassDeviceName)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.macOS)
                return null;

            uint deviceId = findOutputDeviceId(bassDeviceName) ?? getDefaultOutputDeviceId();

            if (deviceId == 0 || !tryGetStreamFormat(deviceId, out AudioStreamBasicDescription asbd))
                return null;

            return ((int)Math.Round(asbd.mSampleRate), (int)asbd.mChannelsPerFrame);
        }

        private static uint? findOutputDeviceId(string? bassDeviceName)
        {
            if (string.IsNullOrWhiteSpace(bassDeviceName))
                return null;

            if (!tryGetDeviceIds(out uint[] deviceIds))
                return null;

            string normalisedTarget = bassDeviceName.Trim();

            foreach (uint deviceId in deviceIds)
            {
                string? name = tryGetDeviceName(deviceId);

                if (string.IsNullOrEmpty(name))
                    continue;

                if (name.Contains(normalisedTarget, StringComparison.OrdinalIgnoreCase)
                    || normalisedTarget.Contains(name, StringComparison.OrdinalIgnoreCase))
                    return deviceId;
            }

            return null;
        }

        private static uint getDefaultOutputDeviceId()
        {
            var address = new AudioObjectPropertyAddress
            {
                mSelector = k_audio_hardware_property_default_output_device,
                mScope = k_audio_object_property_scope_global,
                mElement = k_audio_object_property_element_main,
            };

            uint deviceId = 0;
            uint size = (uint)Marshal.SizeOf<uint>();

            int status = AudioObjectGetPropertyData(k_audio_object_system_object, ref address, 0, IntPtr.Zero, ref size, ref deviceId);

            return status == 0 ? deviceId : 0;
        }

        private static bool tryGetDeviceIds(out uint[] deviceIds)
        {
            deviceIds = Array.Empty<uint>();

            var address = new AudioObjectPropertyAddress
            {
                mSelector = k_audio_hardware_property_devices,
                mScope = k_audio_object_property_scope_global,
                mElement = k_audio_object_property_element_main,
            };

            uint size = 0;
            int status = AudioObjectGetPropertyDataSize(k_audio_object_system_object, ref address, 0, IntPtr.Zero, ref size);

            if (status != 0 || size == 0 || size % sizeof(uint) != 0)
                return false;

            int count = (int)(size / sizeof(uint));
            deviceIds = new uint[count];

            status = AudioObjectGetPropertyData(k_audio_object_system_object, ref address, 0, IntPtr.Zero, ref size, deviceIds);

            return status == 0;
        }

        private static string? tryGetDeviceName(uint deviceId)
        {
            var address = new AudioObjectPropertyAddress
            {
                mSelector = k_audio_object_property_name,
                mScope = k_audio_object_property_scope_global,
                mElement = k_audio_object_property_element_main,
            };

            uint size = 0;
            int status = AudioObjectGetPropertyDataSize(deviceId, ref address, 0, IntPtr.Zero, ref size);

            if (status != 0 || size == 0)
                return null;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);

            try
            {
                status = AudioObjectGetPropertyData(deviceId, ref address, 0, IntPtr.Zero, ref size, buffer);

                if (status != 0)
                    return null;

                IntPtr cfString = size == (uint)IntPtr.Size
                    ? Marshal.ReadIntPtr(buffer)
                    : buffer;

                return cfStringToString(cfString);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string? cfStringToString(IntPtr cfString)
        {
            if (cfString == IntPtr.Zero)
                return null;

            int length = CFStringGetLength(cfString);

            if (length <= 0)
                return string.Empty;

            // UTF-8 buffer with headroom for non-BMP characters.
            int bufferSize = length * 4 + 1;
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

            try
            {
                const uint k_cf_string_encoding_utf8 = 0x08000100;

                if (!CFStringGetCString(cfString, buffer, bufferSize, k_cf_string_encoding_utf8))
                    return null;

                return Marshal.PtrToStringUTF8(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool tryGetStreamFormat(uint deviceId, out AudioStreamBasicDescription asbd)
        {
            asbd = default;

            var address = new AudioObjectPropertyAddress
            {
                mSelector = k_audio_device_property_stream_format,
                mScope = k_audio_object_property_scope_output,
                mElement = k_audio_object_property_element_main,
            };

            uint size = (uint)Marshal.SizeOf<AudioStreamBasicDescription>();
            asbd = new AudioStreamBasicDescription();

            int status = AudioObjectGetPropertyData(deviceId, ref address, 0, IntPtr.Zero, ref size, ref asbd);

            return status == 0 && asbd.mSampleRate > 0;
        }

        private static int bitsFromAsbd(AudioStreamBasicDescription asbd)
        {
            if (asbd.mFormatID == k_audio_format_linear_pcm)
            {
                if ((asbd.mFormatFlags & k_audio_format_flag_is_float) != 0)
                    return 24;

                if (asbd.mBitsPerChannel > 0)
                    return HostAudioFormatQuery.NormaliseBits((int)asbd.mBitsPerChannel);
            }

            if (asbd.mBitsPerChannel > 0)
                return HostAudioFormatQuery.NormaliseBits((int)asbd.mBitsPerChannel);

            return 0;
        }

#pragma warning disable IDE1006 // CoreAudio interop uses Apple's field naming.
        [StructLayout(LayoutKind.Sequential)]
        private struct AudioObjectPropertyAddress
        {
            public uint mSelector;
            public uint mScope;
            public uint mElement;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AudioStreamBasicDescription
        {
            public double mSampleRate;
            public uint mFormatID;
            public uint mFormatFlags;
            public uint mBytesPerPacket;
            public uint mFramesPerPacket;
            public uint mBytesPerFrame;
            public uint mChannelsPerFrame;
            public uint mBitsPerChannel;
            public uint mReserved;
        }
#pragma warning restore IDE1006

        [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio")]
        private static extern int AudioObjectGetPropertyData(uint inObjectID, ref AudioObjectPropertyAddress inAddress, uint inQualifierDataSize, IntPtr inQualifierData, ref uint ioDataSize,
                                                             ref AudioStreamBasicDescription outData);

        [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio")]
        private static extern int AudioObjectGetPropertyData(uint inObjectID, ref AudioObjectPropertyAddress inAddress, uint inQualifierDataSize, IntPtr inQualifierData, ref uint ioDataSize,
                                                             [Out] uint[] outData);

        [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio")]
        private static extern int AudioObjectGetPropertyData(uint inObjectID, ref AudioObjectPropertyAddress inAddress, uint inQualifierDataSize, IntPtr inQualifierData, ref uint ioDataSize,
                                                             IntPtr outData);

        [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio")]
        private static extern int AudioObjectGetPropertyData(uint inObjectID, ref AudioObjectPropertyAddress inAddress, uint inQualifierDataSize, IntPtr inQualifierData, ref uint ioDataSize,
                                                             ref uint outData);

        [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio")]
        private static extern int AudioObjectGetPropertyDataSize(uint inObjectID, ref AudioObjectPropertyAddress inAddress, uint inQualifierDataSize, IntPtr inQualifierData, ref uint outDataSize);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern int CFStringGetLength(IntPtr theString);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern bool CFStringGetCString(IntPtr theString, IntPtr buffer, long bufferSize, uint encoding);
    }
}
