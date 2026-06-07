// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace osu.Framework.Audio
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AsioDeviceInfo
    {
        public IntPtr Name;
        public IntPtr Driver;
    }

    internal delegate int AsioProcedure(bool input, int channel, IntPtr buffer, int length, IntPtr user);

    internal static class BassAsio
    {
        private const string dll_name = "bassasio";

        private static bool? isAvailable;

        public static bool IsAvailable
        {
            get
            {
                if (isAvailable == null)
                {
                    isAvailable = false;

                    if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
                    {
                        try
                        {
                            string arch = Environment.Is64BitProcess ? "win-x64" : "win-x86";
                            string relativePath = Path.Combine("runtimes", arch, "native", "bassasio.dll");
                            string absolutePath = Path.Combine(AppContext.BaseDirectory, relativePath);

                            if (NativeLibrary.TryLoad(absolutePath, out _))
                            {
                                isAvailable = true;
                            }
                            else if (NativeLibrary.TryLoad(dll_name, out _))
                            {
                                isAvailable = true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                return isAvailable.Value;
            }
        }

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_Init")]
        public static extern bool Init(int device, int flags);

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_Free")]
        public static extern bool Free();

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_Start")]
        public static extern bool Start(int buflen);

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_Stop")]
        public static extern bool Stop();

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_GetRate")]
        public static extern double GetRate();

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_ChannelSetFormat")]
        public static extern bool ChannelSetFormat(bool input, int channel, int format);

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_ChannelEnable")]
        public static extern bool ChannelEnable(bool input, int channel, AsioProcedure proc, IntPtr user);

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_ChannelJoin")]
        public static extern bool ChannelJoin(bool input, int channel, int channel2);

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_GetDeviceInfo")]
        public static extern bool GetDeviceInfo(int device, out AsioDeviceInfo info);

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_GetDevice")]
        public static extern int GetDevice();

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_ErrorGetCode")]
        public static extern int ErrorGetCode();

        [DllImport(dll_name, EntryPoint = "BASS_ASIO_GetVersion")]
        public static extern int GetVersion();
    }
}
