// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Logging;
using osuTK;

namespace osu.Framework.Platform.Windows.Native
{
    internal static class HidpUtils
    {
        private static byte[] getPreparsedData(IntPtr device)
        {
            uint payloadSize = 0;
            if (Input.GetRawInputDeviceInfoW(device, 0x20000005, (IntPtr)null, ref payloadSize) == -1)
                Input.ThrowLastError("Unable to get Raw Input Data");
            var preparsedData = new byte[payloadSize];
            if (Input.GetRawInputDeviceInfoW(device, 0x20000005, preparsedData, ref payloadSize) == -1)
                Input.ThrowLastError("Unable to get Raw Input Data");
            return preparsedData;
        }

        private static unsafe IEnumerable<HidpValueCaps> getValueCaps(byte[] preparsedData)
        {
            var status = Input.HidP_GetCaps(preparsedData, out var caps);
            if (status != NSStatus.HIDP_STATUS_SUCCESS)
                throw new NativeException($"Error while getting Value Caps: {status}");

            ushort numValueCaps = caps.NumberInputValueCaps;

            HidpValueCaps[] valueCaps = new HidpValueCaps[numValueCaps];
            if (numValueCaps == 0) return valueCaps;

            byte[] valueData = new byte[numValueCaps * sizeof(HidpValueCaps)];

            status = Input.HidP_GetValueCaps(HidpReportType.HidP_Input, valueData, ref numValueCaps, preparsedData);
            if (status != NSStatus.HIDP_STATUS_SUCCESS)
                throw new NativeException($"Error while getting Value Caps: {status}");

            fixed (byte* valuePtr = valueData)
            {
                for (int i = 0; i < numValueCaps; i++)
                {
                    valueCaps[i] = ((HidpValueCaps*)valuePtr)[i];
                }
            }

            return valueCaps;
        }

        private static unsafe IEnumerable<HidpButtonCaps> getButtonCaps(byte[] preparsedData)
        {
            var status = Input.HidP_GetCaps(preparsedData, out var caps);
            if (status != NSStatus.HIDP_STATUS_SUCCESS)
                throw new NativeException($"Error while getting Button Caps: {status}");

            ushort numButtonCaps = caps.NumberInputButtonCaps;

            HidpButtonCaps[] buttonCaps = new HidpButtonCaps[numButtonCaps];
            if (numButtonCaps == 0) return buttonCaps;

            byte[] buttonData = new byte[numButtonCaps * sizeof(HidpValueCaps)];

            status = Input.HidP_GetButtonCaps(HidpReportType.HidP_Input, buttonData, ref numButtonCaps, preparsedData);
            if (status != NSStatus.HIDP_STATUS_SUCCESS)
                throw new NativeException($"Error while getting Button Caps: {status}");

            fixed (byte* buttonPtr = buttonData)
            {
                for (int i = 0; i < numButtonCaps; i++)
                {
                    buttonCaps[i] = ((HidpButtonCaps*)buttonPtr)[i];
                }
            }

            return buttonCaps;
        }

        public static bool GetHidUsageButton(HidpReportType reportType, HIDUsagePage usagePage, ushort linkCollection, HIDUsage usage, byte[] preparsedData, byte[] report, int reportLength)
        {
            uint numUsages = Input.HidP_MaxUsageListLength(reportType, usagePage, preparsedData);

            ushort[] usages = new ushort[numUsages];

            Input.HidP_GetUsages(reportType, usagePage, linkCollection, usages, ref numUsages, preparsedData, report, reportLength);

            return usages.Any(u => u == (uint)usage);
        }

        public static TouchpadInfo GetDeviceInfo(IntPtr device)
        {
            var preparsedData = getPreparsedData(device);

            Dictionary<ushort, ContactInfo> contacts = new Dictionary<ushort, ContactInfo>();
            ushort linkContactCount = ushort.MaxValue;

            foreach (HidpValueCaps cap in getValueCaps(preparsedData))
            {
                if (cap.IsRange || !cap.IsAbsolute)
                {
                    continue;
                }

                if (!contacts.TryGetValue(cap.LinkCollection, out ContactInfo contact))
                {
                    contact = new ContactInfo { Link = cap.LinkCollection };
                    contacts.Add(cap.LinkCollection, contact);
                }

                switch (cap.UsagePage)
                {
                    case HIDUsagePage.Generic when cap.NotRange.Usage == HIDUsage.HID_USAGE_GENERIC_X:
                        contact.Area.Left = cap.PhysicalMin;
                        contact.Area.Right = cap.PhysicalMax;
                        contact.HasX = true;
                        break;

                    case HIDUsagePage.Generic when cap.NotRange.Usage == HIDUsage.HID_USAGE_GENERIC_Y:
                        contact.Area.Top = cap.PhysicalMin;
                        contact.Area.Bottom = cap.PhysicalMax;
                        contact.HasY = true;
                        break;

                    case HIDUsagePage.Digitizer when cap.NotRange.Usage == HIDUsage.HID_USAGE_DIGITIZER_CONTACT_COUNT:
                        linkContactCount = cap.LinkCollection;
                        break;

                    case HIDUsagePage.Digitizer when cap.NotRange.Usage == HIDUsage.HID_USAGE_DIGITIZER_CONTACT_ID:
                    {
                        Logger.Log(cap.PhysicalMin.ToString());
                        Logger.Log(cap.PhysicalMax.ToString());
                        Logger.Log(cap.LogicalMin.ToString());
                        Logger.Log(cap.LogicalMax.ToString());
                        contact.HasContactID = true;
                        break;
                    }
                }
            }

            foreach (HidpButtonCaps cap in getButtonCaps(preparsedData))
            {
                if (cap.UsagePage == HIDUsagePage.Digitizer)
                {
                    if (cap.NotRange.Usage == HIDUsage.HID_USAGE_DIGITIZER_TIP_SWITCH)
                    {
                        if (contacts.ContainsKey(cap.LinkCollection))
                        {
                            contacts[cap.LinkCollection].HasTip = true;
                        }
                    }
                }
            }

            return new TouchpadInfo
            {
                PreparsedData = preparsedData,
                LinkContactCount = linkContactCount,
                Contacts = contacts.Values.Where(contact => contact.HasX && contact.HasY && contact.HasContactID && contact.HasTip).ToList()
            };
        }

        public static Touch[] GetTouches(TouchpadInfo touchpad, RawHID data)
        {
            Input.HidP_GetUsageValue(HidpReportType.HidP_Input, HIDUsagePage.Digitizer, touchpad.LinkContactCount, HIDUsage.HID_USAGE_DIGITIZER_CONTACT_COUNT, out var numOfContacts, touchpad.PreparsedData, data.RawData, data.DwSizeHid);

            if (numOfContacts > touchpad.Contacts.Count)
            {
                Logger.Log($"More Contacts {numOfContacts} than links {touchpad.Contacts.Count}");
                numOfContacts = (uint)touchpad.Contacts.Count;
            }

            Touch[] touches = new Touch[numOfContacts];

            for (int i = 0; i < numOfContacts; i++)
            {
                ContactInfo contactInfo = touchpad.Contacts[i];

                bool tip = GetHidUsageButton(HidpReportType.HidP_Input, HIDUsagePage.Digitizer, contactInfo.Link, HIDUsage.HID_USAGE_DIGITIZER_TIP_SWITCH, touchpad.PreparsedData, data.RawData,
                    data.DwSizeHid);

                // Indicates that the contact is no longer on the touchpad
                if (!tip)
                {
                    continue;
                }

                Input.HidP_GetUsageValue(HidpReportType.HidP_Input, HIDUsagePage.Digitizer, contactInfo.Link, HIDUsage.HID_USAGE_DIGITIZER_CONTACT_ID, out var id, touchpad.PreparsedData, data.RawData, data.DwSizeHid);
                Input.HidP_GetScaledUsageValue(HidpReportType.HidP_Input, HIDUsagePage.Generic, contactInfo.Link, HIDUsage.HID_USAGE_GENERIC_X, out var x, touchpad.PreparsedData, data.RawData, data.DwSizeHid);
                Input.HidP_GetScaledUsageValue(HidpReportType.HidP_Input, HIDUsagePage.Generic, contactInfo.Link, HIDUsage.HID_USAGE_GENERIC_Y, out var y, touchpad.PreparsedData, data.RawData,
                    data.DwSizeHid);
                touches[i] = new Touch((TouchSource)id, new Vector2(x, y));
            }

            return touches;
        }

        public static Touch GetPrimaryTouch(IEnumerable<Touch> touches)
        {
            return touches.First(touch => touch.Source == TouchSource.Touch1);
        }

        public static Touch MapToScreen(TouchArea area, Touch touch)
        {
            int deltaX = (int)(touch.Position.X - area.Left);
            int deltaY = (int)(touch.Position.Y - area.Top);

            // As per HID spec, maximum is inclusive, so we need to add 1 here
            int tpWidth = area.Right + 1 - area.Left;
            int tpHeight = area.Bottom + 1 - area.Top;

            float scDeltaX = (float)deltaX / tpWidth;
            float scDeltaY = (float)deltaY / tpHeight;

            return new Touch(touch.Source, new Vector2(scDeltaX, scDeltaY));
        }

        public struct TouchpadInfo
        {
            public byte[] PreparsedData;

            public ushort LinkContactCount;

            // The short is the link
            public List<ContactInfo> Contacts;
        }

        public class ContactInfo
        {
            public ushort Link;
            public TouchArea Area;

            public bool HasX;
            public bool HasY;
            public bool HasContactID;
            public bool HasTip;
        }

        public struct TouchArea
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }
    }

    public enum NSStatus : uint
    {
        HIDP_STATUS_SUCCESS = 0x00110000,
        HIDP_STATUS_INVALID_PREPARSED_DATA = 0xc0110001,
        HIDP_STATUS_USAGE_NOT_FOUND = 0xc0110004
    }
}
