// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming
// (We are using the original names from the Windows API with type prefix removed.)

namespace osu.Framework.Platform.Windows.Native
{
    internal class Hid
    {
        public const long HIDP_STATUS_SUCCESS = 0x00110000;

        [DllImport("hid.dll")]
        public static extern long HidP_GetCaps(IntPtr PreparsedData, out HIDP_CAPS Capabilities);

        [DllImport("hid.dll")]
        public static extern long HidP_GetValueCaps(HIDP_REPORT_TYPE ReportType, [Out] HIDP_VALUE_CAPS[] ValueCaps, ref ulong ValueCapsLength, IntPtr PreparsedData);

        [DllImport("hid.dll")]
        public static extern long HidP_GetLinkCollectionNodes([Out] HIDP_LINK_COLLECTION_NODE[] LinkCollectionNodes, ref ulong LinkCollectionNodesLength, IntPtr PreparsedData);

        [DllImport("hid.dll")]
        public static extern long HidP_GetUsageValue(
            HIDP_REPORT_TYPE ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out ulong UsageValue,
            IntPtr PreparsedData, byte[] Report, ulong ReportLength);

        [DllImport("hid.dll")]
        public static extern long HidP_GetUsagesEx(
            HIDP_REPORT_TYPE ReportType, ushort LinkCollection, [Out] USAGE_AND_PAGE[] ButtonList, ref ulong UsageLength,
            IntPtr PreparsedData, byte[] Report, ulong ReportLength);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        private unsafe fixed ushort Reserved[17];

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

    public enum HIDP_REPORT_TYPE
    {
        HidP_Input,
        HidP_Output,
        HidP_Feature
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HIDP_VALUE_CAPS
    {
        public ushort UsagePage;
        public byte ReportID;
        public byte IsAlias;

        public ushort BitField;
        public ushort LinkCollection;

        public ushort LinkUsage;
        public ushort LinkUsagePage;

        public byte IsRange;
        public byte IsStringRange;
        public byte IsDesignatorRange;
        public byte IsAbsolute;

        public byte HasNull;
        private byte Reserved;
        public ushort BitSize;

        public ushort ReportCount;
        private unsafe fixed ushort Reserved2[5];

        public uint UnitsExp;
        public uint Units;

        public int LogicalMin, LogicalMax;
        public int PhysicalMin, PhysicalMax;
        public Union union;

        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        public struct Union
        {
            [FieldOffset(0)]
            public _Range Range;

            [FieldOffset(0)]
            public _NotRange NotRange;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct _Range
        {
            public ushort UsageMin, UsageMax;
            public ushort StringMin, StringMax;
            public ushort DesignatorMin, DesignatorMax;
            public ushort DataIndexMin, DataIndexMax;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct _NotRange
        {
            public ushort Usage;
            private ushort Reserved1;
            public ushort StringIndex;
            private ushort Reserved2;
            public ushort DesignatorIndex;
            private ushort Reserved3;
            public ushort DataIndex;
            private ushort Reserved4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HIDP_LINK_COLLECTION_NODE
    {
        public ushort LinkUsage;
        public ushort LinkUsagePage;
        public ushort Parent;
        public ushort NumberOfChildren;
        public ushort NextSibling;
        public ushort FirstChild;

        // The original definition is:
        //     ULONG    CollectionType: 8;  // As defined in 6.2.2.6 of HID spec
        //     ULONG    IsAlias : 1; // This link node is an allias of the next link node.
        //     ULONG    Reserved: 23;
        // Fortunately the value is not used here. Don't bother parsing the bitfield now.
        public UInt32 _bitfield;

        public IntPtr UserContext;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct USAGE_AND_PAGE
    {
        public ushort Usage;
        public ushort UsagePage;
    }
}
