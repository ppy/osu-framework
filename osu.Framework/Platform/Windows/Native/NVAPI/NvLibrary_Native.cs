// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

#nullable disable

namespace osu.Framework.Platform.Windows.Native.NVAPI
{
    public partial class NvLibrary
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [NvInterface(0x150E828)]
        private delegate NvStatus InitInterfaceDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [NvInterface(0x694D52E)]
        private delegate NvStatus DrsCreateSessionDelegate(out IntPtr sessionHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [NvInterface(0xDAD9CFF8)]
        private delegate NvStatus DrsDestroySessionDelegate(IntPtr sessionHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [NvInterface(0x375DBD6B)]
        private delegate NvStatus DrsLoadSettingsDelegate(IntPtr sessionHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [NvInterface(0xEEE566B2)]
        private delegate NvStatus DrsFindApplicationByNameDelegate(IntPtr sessionHandle, [MarshalAs(UnmanagedType.LPWStr)] string appName,
                                                                   out IntPtr profileHandle, ref NvDrsApplicationV1 application);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [NvInterface(0xCC176068)]
        private delegate NvStatus DrsCreateProfileDelegate(IntPtr sessionHandle, ref NvDrsProfileV1 profile, out IntPtr profileHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [NvInterface(0x4347A9DE)]
        private delegate NvStatus DrsCreateApplicationDelegate(IntPtr sessionHandle, IntPtr profileHandle, ref NvDrsApplicationV1 application);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [NvInterface(0x577DD202)]
        private delegate NvStatus DrsSetSettingDelegate(IntPtr sessionHandle, IntPtr profileHandle, ref NvDrsSettingV1 setting);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [NvInterface(0xFCBC7E14)]
        private delegate NvStatus DrsSaveSettingsDelegate(IntPtr sessionHandle);

        private static InitInterfaceDelegate initInterface;
        private static DrsCreateSessionDelegate drsCreateSession;
        private static DrsDestroySessionDelegate drsDestroySession;
        private static DrsLoadSettingsDelegate drsLoadSettings;
        private static DrsFindApplicationByNameDelegate drsFindApplicationByName;
        private static DrsCreateProfileDelegate drsCreateProfile;
        private static DrsCreateApplicationDelegate drsCreateApplication;
        private static DrsSetSettingDelegate drsSetSetting;
        private static DrsSaveSettingsDelegate drsSaveSettings;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    public struct NvDrsProfileV1
    {
        public uint Version;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x800)]
        public string ProfileName;

        public uint GpuSupport;
        public uint IsPredefined;
        public uint NumOfApps;
        public uint NumOfSettings;

        public static uint TargetVersion => (uint)Marshal.SizeOf(typeof(NvDrsProfileV1)) | (1 << 16);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    public struct NvDrsApplicationV1
    {
        public uint Version;
        public uint IsPredefined;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x800)]
        public string AppName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x800)]
        public string UserFriendlyName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x800)]
        public string Launcher;

        public static uint TargetVersion => (uint)Marshal.SizeOf(typeof(NvDrsApplicationV1)) | (1 << 16);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct NvDrsSettingValue
    {
        public uint U32Value;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1000, ArraySubType = UnmanagedType.U8)]
        private readonly byte[] pad;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    public struct NvDrsSettingV1
    {
        public uint Version;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
        public string SettingName;

        public NvSettingId SettingId;
        public NvSettingType SettingType;
        public NvSettingLocation SettingLocation;
        public uint IsCurrentPredefined;
        public uint IsPredefinedValid;
        public NvDrsSettingValue PredefinedValue;
        public NvDrsSettingValue CurrentValue;

        public static uint TargetVersion => (uint)Marshal.SizeOf(typeof(NvDrsSettingV1)) | (1 << 16);
    }

    public enum NvSettingId : uint
    {
        OGL_THREAD_CONTROL_ID = 0x20C1221E
    }

    public enum NvSettingType : uint
    {
        DWORD = 0,
        BINARY = 1,
        STRING = 2,
        WSTRING = 3,
    }

    public enum NvSettingLocation : uint
    {
        CURRENT = 0,
        GLOBAL = 1,
        BASE = 2,
        DEFAULT = 3,
    }
}
