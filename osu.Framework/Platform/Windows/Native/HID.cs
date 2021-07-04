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
    internal static class HID
    {
        public static byte[] GetPreparsedData(IntPtr device)
        {
            uint payloadSize = 0;
            if (Input.GetRawInputDeviceInfoW(device, 0x20000005, (IntPtr)null, ref payloadSize) == -1)
                Logger.Log("Something broken!");
            var preparsedData = new byte[payloadSize];
            if (Input.GetRawInputDeviceInfoW(device, 0x20000005, preparsedData, ref payloadSize) == -1)
                Logger.Log("Something broken!");
            return preparsedData;
        }

        public static unsafe HidpValueCaps[] GetValueCaps(byte[] preparsedData)
        {
            var status = Input.HidP_GetCaps(preparsedData, out var caps);
            if (status != NSStatus.HIDP_STATUS_SUCCESS)
                Logger.Log("Something ain't right: " + status);

            ushort numValueCaps = caps.NumberInputValueCaps;

            HidpValueCaps[] valueCaps = new HidpValueCaps[numValueCaps];
            if (numValueCaps == 0) return valueCaps;

            byte[] valueData = new byte[numValueCaps * sizeof(HidpValueCaps)];

            status = Input.HidP_GetValueCaps(HidpReportType.HidP_Input, valueData, ref numValueCaps, preparsedData);
            if (status != NSStatus.HIDP_STATUS_SUCCESS)
                Logger.Log("Something ain't right: " + status);

            fixed (byte* valuePtr = valueData)
            {
                for (int i = 0; i < numValueCaps; i++)
                {
                    valueCaps[i] = ((HidpValueCaps*)valuePtr)[i];
                }
            }

            return valueCaps;
        }

        public static unsafe HidpButtonCaps[] GetButtonCaps(byte[] preparsedData)
        {
            var status = Input.HidP_GetCaps(preparsedData, out var caps);
            if (status != NSStatus.HIDP_STATUS_SUCCESS)
                Logger.Log("Something ain't right: " + status);

            ushort numButtonCaps = caps.NumberInputButtonCaps;

            HidpButtonCaps[] buttonCaps = new HidpButtonCaps[numButtonCaps];
            if (numButtonCaps == 0) return buttonCaps;

            byte[] buttonData = new byte[numButtonCaps * sizeof(HidpValueCaps)];

            status = Input.HidP_GetButtonCaps(HidpReportType.HidP_Input, buttonData, ref numButtonCaps, preparsedData);
            if (status != NSStatus.HIDP_STATUS_SUCCESS)
                Logger.Log("Something ain't right: " + status);

            fixed (byte* buttonPtr = buttonData)
            {
                for (int i = 0; i < numButtonCaps; i++)
                {
                    buttonCaps[i] = ((HidpButtonCaps*)buttonPtr)[i];
                }
            }

            return buttonCaps;
        }

        public static TouchpadInfo GetDeviceInfo(IntPtr device)
        {
            var preparsedData = GetPreparsedData(device);

            // Button Caps, has tips into something like a dictionary and then read below here.

            Dictionary<ushort, ContactInfo> contacts = new Dictionary<ushort, ContactInfo>();
            ushort linkContactCount = ushort.MaxValue;

            foreach (HidpValueCaps cap in GetValueCaps(preparsedData))
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
                    // Supposed to be PhysicalMin and Max, but they were too low of a value.
                    case HIDUsagePage.Generic when cap.NotRange.Usage == HIDUsage.HID_USAGE_GENERIC_X:
                        contact.Area.Left = cap.LogicalMin;
                        contact.Area.Right = cap.LogicalMax;
                        contact.HasX = true;
                        break;

                    case HIDUsagePage.Generic when cap.NotRange.Usage == HIDUsage.HID_USAGE_GENERIC_Y:
                        contact.Area.Top = cap.LogicalMin;
                        contact.Area.Bottom = cap.LogicalMax;
                        contact.HasY = true;
                        break;

                    case HIDUsagePage.Digitizer when cap.NotRange.Usage == HIDUsage.HID_USAGE_DIGITIZER_CONTACT_COUNT:
                        linkContactCount = cap.LinkCollection;
                        break;

                    case HIDUsagePage.Digitizer when cap.NotRange.Usage == HIDUsage.HID_USAGE_DIGITIZER_CONTACT_ID:
                    {
                        contact.HasContactID = true;
                        break;
                    }

                    case HIDUsagePage.Digitizer when cap.NotRange.Usage == HIDUsage.HID_USAGE_DIGITZER_WIDTH:
                    {
                        Logger.Log($"WIDTHS: {cap.PhysicalMin} {cap.PhysicalMax}");
                        break;
                    }

                    case HIDUsagePage.Digitizer when cap.NotRange.Usage == HIDUsage.HID_USAGE_DIGITZER_HEIGHT:
                    {
                        Logger.Log($"HEIGHT: {cap.PhysicalMin} {cap.PhysicalMax}");
                        break;
                    }
                }
            }

            foreach (HidpButtonCaps cap in GetButtonCaps(preparsedData))
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

        public struct TouchpadInfo
        {
            public byte[] PreparsedData;

            public ushort LinkContactCount;

            // The short is the link
            public List<ContactInfo> Contacts;
        }

        public class ContactInfo
        {
            // https://docs.microsoft.com/en-us/windows-hardware/design/component-guidelines/supporting-usages-in-multitouch-digitizer-drivers
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

        public static Touch[] GetContacts(TouchpadInfo touchpad, RawHID data)
        {
            Input.HidP_GetUsageValue(HidpReportType.HidP_Input, HIDUsagePage.Digitizer, touchpad.LinkContactCount, HIDUsage.HID_USAGE_DIGITIZER_CONTACT_COUNT, out var numOfContacts, touchpad.PreparsedData, data.rawData, data.dwSizeHid);

            if (numOfContacts > touchpad.Contacts.Count)
            {
                Logger.Log($"More Contacts {numOfContacts} than links {touchpad.Contacts.Count}");
                numOfContacts = (uint)touchpad.Contacts.Count;
            }

            Touch[] touches = new Touch[numOfContacts];

            for (int i = 0; i < numOfContacts; i++)
            {
                ContactInfo contactInfo = touchpad.Contacts[i];

                bool tip = Input.GetHidUsageButton(HidpReportType.HidP_Input, HIDUsagePage.Digitizer, contactInfo.Link, HIDUsage.HID_USAGE_DIGITIZER_TIP_SWITCH, touchpad.PreparsedData, data.rawData,
                    data.dwSizeHid);

                if (!tip)
                {
                    Logger.Log("Contact tip is false");
                    continue;
                }

                Input.HidP_GetUsageValue(HidpReportType.HidP_Input, HIDUsagePage.Digitizer, contactInfo.Link, HIDUsage.HID_USAGE_DIGITIZER_CONTACT_ID, out var id, touchpad.PreparsedData, data.rawData, data.dwSizeHid);
                Input.HidP_GetUsageValue(HidpReportType.HidP_Input, HIDUsagePage.Generic, contactInfo.Link, HIDUsage.HID_USAGE_GENERIC_X, out var x, touchpad.PreparsedData, data.rawData, data.dwSizeHid);
                Input.HidP_GetUsageValue(HidpReportType.HidP_Input, HIDUsagePage.Generic, contactInfo.Link, HIDUsage.HID_USAGE_GENERIC_Y, out var y, touchpad.PreparsedData, data.rawData,
                    data.dwSizeHid);
                touches[i] = new Touch((TouchSource)id, new Vector2(x, y));
            }

            return touches;
        }

        public static Touch GetPrimaryTouch(Touch[] touches)
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

            int scDeltaX = (deltaX << 16) / tpWidth;
            int scDeltaY = (deltaY << 16) / tpHeight;

            return new Touch(touch.Source, new Vector2((float)scDeltaX / 0xFFFF, (float)scDeltaY / 65535));
        }
    }
}
