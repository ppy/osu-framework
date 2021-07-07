// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HidpCaps
    {
        public HIDUsage Usage;
        public HIDUsagePage UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        public fixed ushort Reserved[17];
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct HidpButtonCaps
    {
        [FieldOffset(0)]
        public HIDUsagePage UsagePage;

        [FieldOffset(2)]
        public byte ReportID;

        [FieldOffset(3), MarshalAs(UnmanagedType.U1)]
        public bool IsAlias;

        [FieldOffset(4)]
        public ushort BitField;

        [FieldOffset(6)]
        public ushort LinkCollection;

        [FieldOffset(8)]
        public HIDUsage LinkUsage;

        [FieldOffset(10)]
        public HIDUsage LinkUsagePage;

        [FieldOffset(12), MarshalAs(UnmanagedType.U1)]
        public bool IsRange;

        [FieldOffset(13), MarshalAs(UnmanagedType.U1)]
        public bool IsStringRange;

        [FieldOffset(14), MarshalAs(UnmanagedType.U1)]
        public bool IsDesignatorRange;

        [FieldOffset(15), MarshalAs(UnmanagedType.U1)]
        public bool IsAbsolute;

        [FieldOffset(16)]
        public ushort ReportCount;

        [FieldOffset(18)]
        public ushort Reserved2;

        [FieldOffset(20)]
        public fixed uint Reserved[10];

        [FieldOffset(56)]
        public Range Range;

        [FieldOffset(56)]
        public NonRange NotRange;
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct HidpValueCaps
    {
        [FieldOffset(0)]
        public HIDUsagePage UsagePage;

        [FieldOffset(2)]
        public byte ReportID;

        [FieldOffset(3), MarshalAs(UnmanagedType.U1)]
        public bool IsAlias;

        [FieldOffset(4)]
        public ushort BitField;

        [FieldOffset(6)]
        public ushort LinkCollection;

        [FieldOffset(8)]
        public HIDUsage LinkUsage;

        [FieldOffset(10)]
        public HIDUsage LinkUsagePage;

        [FieldOffset(12), MarshalAs(UnmanagedType.U1)]
        public bool IsRange;

        [FieldOffset(13), MarshalAs(UnmanagedType.U1)]
        public bool IsStringRange;

        [FieldOffset(14), MarshalAs(UnmanagedType.U1)]
        public bool IsDesignatorRange;

        [FieldOffset(15), MarshalAs(UnmanagedType.U1)]
        public bool IsAbsolute;

        [FieldOffset(16), MarshalAs(UnmanagedType.U1)]
        public bool HasNull;

        [FieldOffset(17)]
        public byte Reserved;

        [FieldOffset(18)]
        public ushort BitSize;

        [FieldOffset(20)]
        public ushort ReportCount;

        [FieldOffset(22)]
        public fixed ushort Reserved2[5];

        [FieldOffset(32)]
        public uint UnitsExp;

        [FieldOffset(36)]
        public uint Units;

        [FieldOffset(40)]
        public int LogicalMin;

        [FieldOffset(44)]
        public int LogicalMax;

        [FieldOffset(48)]
        public int PhysicalMin;

        [FieldOffset(52)]
        public int PhysicalMax;

        [FieldOffset(56)]
        public Range Range;

        [FieldOffset(56)]
        public NonRange NotRange;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Range
    {
        public HIDUsage UsageMin, UsageMax;
        public ushort StringMin, StringMax;
        public ushort DesignatorMin, DesignatorMax;
        public ushort DataIndexMin, DataIndexMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NonRange
    {
        public HIDUsage Usage, Reserved1;
        public ushort StringIndex, Reserved2;
        public ushort DesignatorIndex, Reserved3;
        public ushort DataIndex, Reserved4;
    }
}
