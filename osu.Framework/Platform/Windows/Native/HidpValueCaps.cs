// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows.Native
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct HidpValueCaps
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

        [FieldOffset(16), MarshalAs(UnmanagedType.U1)]
        public bool HasNull;

        // Supposed to be uchar
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

        public override string ToString()
            => $"{nameof(UsagePage)}: {UsagePage}, {nameof(ReportID)}: {ReportID}, {nameof(IsAlias)}: {IsAlias}, {nameof(BitField)}: {BitField}, {nameof(LinkCollection)}: {LinkCollection}, {nameof(LinkUsage)}: {LinkUsage}, {nameof(LinkUsagePage)}: {LinkUsagePage}, {nameof(IsRange)}: {IsRange}, {nameof(IsStringRange)}: {IsStringRange}, {nameof(IsDesignatorRange)}: {IsDesignatorRange}, {nameof(IsAbsolute)}: {IsAbsolute}, {nameof(HasNull)}: {HasNull}, {nameof(Reserved)}: {Reserved}, {nameof(BitSize)}: {BitSize}, {nameof(ReportCount)}: {ReportCount}, {nameof(UnitsExp)}: {UnitsExp}, {nameof(Units)}: {Units}, {nameof(LogicalMin)}: {LogicalMin}, {nameof(LogicalMax)}: {LogicalMax}, {nameof(PhysicalMin)}: {PhysicalMin}, {nameof(PhysicalMax)}: {PhysicalMax}, {nameof(Range)}: {Range}, {nameof(NotRange)}: {NotRange}";
    }

    public struct Range
    {
        public HIDUsage UsageMin, UsageMax;
        public ushort StringMin, StringMax;
        public ushort DesignatorMin, DesignatorMax;
        public ushort DataIndexMin, DataIndexMax;

        public override string ToString()
        {
            return $"{nameof(UsageMin)}: {UsageMin}, {nameof(UsageMax)}: {UsageMax}, {nameof(StringMin)}: {StringMin}, {nameof(StringMax)}: {StringMax}, {nameof(DesignatorMin)}: {DesignatorMin}, {nameof(DesignatorMax)}: {DesignatorMax}, {nameof(DataIndexMin)}: {DataIndexMin}, {nameof(DataIndexMax)}: {DataIndexMax}";
        }
    }

    public struct NonRange
    {
        public HIDUsage Usage, Reserved1;
        public ushort StringIndex,  Reserved2;
        public ushort DesignatorIndex, Reserved3;
        public ushort DataIndex, Reserved4;

        public override string ToString()
        {
            return $"{nameof(Usage)}: {Usage}, {nameof(Reserved1)}: {Reserved1}, {nameof(StringIndex)}: {StringIndex}, {nameof(Reserved2)}: {Reserved2}, {nameof(DesignatorIndex)}: {DesignatorIndex}, {nameof(Reserved3)}: {Reserved3}, {nameof(DataIndex)}: {DataIndex}, {nameof(Reserved4)}: {Reserved4}";
        }
    }
}
