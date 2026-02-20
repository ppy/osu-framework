// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Logging;
using osu.Framework.Platform.Windows.Native;

namespace osu.Framework.Platform.Windows
{
    internal class WindowsTouchpadReader
    {
        public event Action<TouchpadData>? TouchpadDataUpdate;

        public WindowsTouchpadReader(WindowsRawInputManager rawInputManager)
        {
            rawInputManager.RawTouchpad += readTouchpad;
        }

        /// <summary>The actual report reader for one single device, contains parsed information.</summary>
        private TouchpadInstanceReader? reader;

        private void readTouchpad(RawInputDataHidHeader header, List<byte[]> reports)
        {
            if (reader?.Info.Handle != header.Header.Device)
            {
                // TODO: If we enable more raw input devices later, it can be possible to run this code every time when reports for another device is received.
                //  For the best result, extract the device manager code into a new class (and cache all connected devices)
                if (!prepareDevice(header.Header.Device))
                    return;
            }

            TouchpadData? data = null;

            foreach (byte[] report in reports)
            {
                data = reader!.ReadRawInput(report);
            }

            if (data.HasValue)
                TouchpadDataUpdate?.Invoke(data.Value);
        }

        private unsafe bool prepareDevice(IntPtr hDevice)
        {
            RID_DEVICE_INFO deviceInfo = new RID_DEVICE_INFO();
            int size = deviceInfo.Size = sizeof(RID_DEVICE_INFO);

            if (Native.Input.GetRawInputDeviceInfo(hDevice, RawInputDeviceInfoCommand.DeviceInfo, ref deviceInfo, ref size) == -1)
            {
                Logger.Log($"GetRawInputDeviceInfo failed ({Marshal.GetLastWin32Error()})", LoggingTarget.Input, LogLevel.Error);
                return false;
            }

            if (deviceInfo.Type != RawInputType.HID
                || deviceInfo.union.hid.UsagePage != (int)HIDUsagePage.Digitizer
                || deviceInfo.union.hid.Usage != (int)HIDUsage.TouchPad)
                return false;

            try
            {
                reader = new TouchpadInstanceReader(hDevice);
            }
            catch (TouchpadInstanceReader.ParseException e)
            {
                Logger.Error(e, "TouchpadReader creation failed: cannot parse device info", LoggingTarget.Input);
                return false;
            }

            return true;
        }

        private class TouchpadInstanceReader
        {
            public class ParseException : Exception
            {
                public ParseException(string? message)
                    : base(message)
                {
                }
            }

            public class ReadException : Exception
            {
                public ReadException(string? message)
                    : base(message)
                {
                }
            }

            /// <summary>Information read from the touchpad.</summary>
            public readonly TouchpadInfo Info;

            private readonly IntPtr preparsedData;

            /// <summary>Which LCs are corresponding to the fingers</summary>
            private readonly List<ushort> fingerLinkCollections;

            private readonly USAGE_AND_PAGE[] usageAndPageBuffer;

            public TouchpadInstanceReader(IntPtr hDevice)
            {
                Info.Handle = hDevice;

                // Read preparsed_data
                int size = 0;
                if (Native.Input.GetRawInputDeviceInfo(hDevice, RawInputDeviceInfoCommand.PreparsedData, IntPtr.Zero, ref size) != 0)
                    throw new ParseException($"GetRawInputDeviceInfo(RIDI_PREPARSEDDATA): {Marshal.GetLastWin32Error()}");

                preparsedData = Marshal.AllocHGlobal(size);
                if (Native.Input.GetRawInputDeviceInfo(hDevice, RawInputDeviceInfoCommand.PreparsedData, preparsedData, ref size) == -1)
                    throw new ParseException($"GetRawInputDeviceInfo(RIDI_PREPARSEDDATA)2: {Marshal.GetLastWin32Error()}");

                // Read caps (i.e. summary information of the device)
                HIDP_CAPS caps;
                if (Hid.HidP_GetCaps(preparsedData, out caps) != Hid.HIDP_STATUS_SUCCESS)
                    throw new ParseException($"HidP_GetCaps: {Marshal.GetLastWin32Error()}");

                // Read valueCaps (i.e. information about every reported numeral value)
                ulong valueCapsLength = caps.NumberInputValueCaps;
                var valueCaps = new HIDP_VALUE_CAPS[valueCapsLength];
                if (Hid.HidP_GetValueCaps(HIDP_REPORT_TYPE.HidP_Input, valueCaps, ref valueCapsLength, preparsedData) != Hid.HIDP_STATUS_SUCCESS)
                    throw new ParseException($"HidP_GetValueCaps: {Marshal.GetLastWin32Error()}");
                if (valueCapsLength != caps.NumberInputValueCaps)
                    throw new ParseException($"NumberInputValueCaps mismatch, before: {caps.NumberInputValueCaps} after: {valueCapsLength}");

                // Read linkCollections to find out which LC corresponds to fingers.
                // (LC: a collection of UsagePage and Usages, one for each finger contact, and one for the touchpad global info)
                ulong linkCollectionLength = caps.NumberLinkCollectionNodes;
                var lcList = new HIDP_LINK_COLLECTION_NODE[linkCollectionLength];
                if (Hid.HidP_GetLinkCollectionNodes(lcList, ref linkCollectionLength, preparsedData) != Hid.HIDP_STATUS_SUCCESS)
                    throw new ParseException($"HidP_GetLinkCollectionNodes: {Marshal.GetLastWin32Error()}");
                if (linkCollectionLength != caps.NumberLinkCollectionNodes)
                    throw new ParseException($"NumberLinkCollectionNodes mismatch, before: {caps.NumberLinkCollectionNodes} after: {linkCollectionLength}");

                fingerLinkCollections = new List<ushort>();
                ushort index = lcList[0].FirstChild;

                while (index != 0)
                {
                    var lc = lcList[index];

                    // 0x0D, 0x22: Finger
                    if (lc.LinkUsagePage == 0x0D && lc.LinkUsage == 0x22)
                        fingerLinkCollections.Add(index);

                    index = lc.NextSibling;
                }

                fingerLinkCollections.Sort();

                usageAndPageBuffer = new USAGE_AND_PAGE[caps.NumberInputButtonCaps];

                int maxFingerCount = fingerLinkCollections.Count;
                if (maxFingerCount <= 0)
                    throw new ParseException($"Invalid finger count: {maxFingerCount}");

                foreach (var valueCap in valueCaps)
                {
                    // Just read the XY ranges for the first finger.
                    // Never seen a device with different ranges for fingers.
                    if (valueCap.LinkCollection != fingerLinkCollections[0]) continue;

                    // 0x01, 0x30: X
                    if (checkUsageMatch(valueCap, 0x01, 0x30))
                    {
                        Info.XMin = valueCap.LogicalMin;
                        Info.XRange = valueCap.LogicalMax - valueCap.LogicalMin;
                    }

                    // 0x01, 0x31: Y
                    if (checkUsageMatch(valueCap, 0x01, 0x31))
                    {
                        Info.YMin = valueCap.LogicalMin;
                        Info.YRange = valueCap.LogicalMax - valueCap.LogicalMin;
                    }
                }
            }

            private static bool checkUsageMatch(HIDP_VALUE_CAPS valueCap, ushort usagePage, ushort usage)
            {
                if (valueCap.UsagePage != usagePage)
                    return false;

                return valueCap.IsRange != 0
                    ? valueCap.union.Range.UsageMin <= usage && usage <= valueCap.union.Range.UsageMax
                    : valueCap.union.NotRange.Usage == usage;
            }

            ~TouchpadInstanceReader()
            {
                Marshal.FreeHGlobal(preparsedData);
            }

            public TouchpadData ReadRawInput(byte[] report)
            {
                uint reportLen = (uint)report.Length;

                // TODO: comment on the usage values
                ulong fingerCount;
                if (Hid.HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, 0x0D, 0, 0x54, out fingerCount, preparsedData, report, reportLen) != Hid.HIDP_STATUS_SUCCESS)
                    throw new ReadException($"HidP_GetUsageValue (lc=0,0D:54): {Marshal.GetLastWin32Error()}");

                int validPointCount = Math.Min((int)fingerCount, fingerLinkCollections.Count);

                // TODO do we care about scan_time?
                // 0D:56 scan time in 100us units, can be unavailable

                ulong caplen = (ulong)usageAndPageBuffer.Length;

                if (Hid.HidP_GetUsagesEx(HIDP_REPORT_TYPE.HidP_Input, 0, usageAndPageBuffer, ref caplen, preparsedData, report, reportLen) != Hid.HIDP_STATUS_SUCCESS)
                    throw new ReadException($"HidP_GetUsagesEx (lc=0): {Marshal.GetLastWin32Error()}");

                bool buttonDown = false;

                for (ulong i = 0; i < caplen; i++)
                {
                    ushort usagePage = usageAndPageBuffer[i].UsagePage;
                    ushort usage = usageAndPageBuffer[i].Usage;

                    if (usagePage == 0x09 && usage == 0x01)
                        buttonDown = true;
                }

                List<TouchpadPoint> points = new List<TouchpadPoint>();

                for (int i = 0; i < validPointCount; i++)
                {
                    TouchpadPoint point = new TouchpadPoint();
                    ushort lc = fingerLinkCollections[i];
                    ulong temp;

                    if (Hid.HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, 0x01, lc, 0x30, out temp, preparsedData, report, reportLen) != Hid.HIDP_STATUS_SUCCESS)
                        throw new ReadException($"HidP_GetUsageValue (lc={lc},01:30): {Marshal.GetLastWin32Error()}");

                    point.X = (int)temp;

                    if (Hid.HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, 0x01, lc, 0x31, out temp, preparsedData, report, reportLen) != Hid.HIDP_STATUS_SUCCESS)
                        throw new ReadException($"HidP_GetUsageValue (lc={lc},01:31): {Marshal.GetLastWin32Error()}");

                    point.Y = (int)temp;

                    if (Hid.HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, 0x0D, lc, 0x51, out temp, preparsedData, report, reportLen) != Hid.HIDP_STATUS_SUCCESS)
                        throw new ReadException($"HidP_GetUsageValue (lc={lc},0D:51): {Marshal.GetLastWin32Error()}");

                    point.ContactId = (int)temp;

                    caplen = (ulong)usageAndPageBuffer.Length;

                    if (Hid.HidP_GetUsagesEx(HIDP_REPORT_TYPE.HidP_Input, lc, usageAndPageBuffer, ref caplen, preparsedData, report, reportLen) != Hid.HIDP_STATUS_SUCCESS)
                        throw new ReadException($"HidP_GetUsagesEx (lc={lc}): {Marshal.GetLastWin32Error()}");

                    point.Valid = point.Confidence = false;

                    for (int j = 0; j < (int)caplen; j++)
                    {
                        ushort usagePage = usageAndPageBuffer[j].UsagePage;
                        ushort usage = usageAndPageBuffer[j].Usage;

                        if (usagePage == 0x0D && usage == 0x42)
                            point.Valid = true;
                        if (usagePage == 0x0D && usage == 0x47)
                            point.Confidence = true;
                    }

                    points.Add(point);
                }

                return new TouchpadData(Info, points, buttonDown);
            }
        }
    }
}
