// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows.Native
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct HidpButtonCaps
    {
        [FieldOffset(0)]
        public HIDUsagePage UsagePage;

        // Supposed to be uchar
        [FieldOffset(2)]
        public byte ReportID;

        [FieldOffset(3), MarshalAs(UnmanagedType.U1)]
        public bool IsAlias;

        [FieldOffset(4)]
        public ushort BitField;

        [FieldOffset(6)]
        public ushort LinkCollection;

        // Supposed to be a different usage
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

        // Supposed to be uchar
        [FieldOffset(18)]
        public ushort Reserved2;

        [FieldOffset(20)]
        public fixed uint Reserved[10];

        [FieldOffset(56)]
        public Range Range;

        [FieldOffset(56)]
        public NonRange NotRange;

        public override string ToString()
        {
            return $"{nameof(UsagePage)}: {UsagePage}, {nameof(ReportID)}: {ReportID}, {nameof(IsAlias)}: {IsAlias}, {nameof(BitField)}: {BitField}, {nameof(LinkCollection)}: {LinkCollection}, {nameof(LinkUsage)}: {LinkUsage}, {nameof(LinkUsagePage)}: {LinkUsagePage}, {nameof(IsRange)}: {IsRange}, {nameof(IsStringRange)}: {IsStringRange}, {nameof(IsDesignatorRange)}: {IsDesignatorRange}, {nameof(IsAbsolute)}: {IsAbsolute}, {nameof(ReportCount)}: {ReportCount}, {nameof(Reserved2)}: {Reserved2}, {nameof(Range)}: {Range}, {nameof(NotRange)}: {NotRange}";
        }
    }
}
