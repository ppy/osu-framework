// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows.Native
{
    internal static class Input
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool SetWindowFeedbackSetting(IntPtr hwnd, FeedbackType feedback, ulong flags, uint size, int* configuration);

        public static unsafe void SetWindowFeedbackSetting(IntPtr hwnd, FeedbackType feedback, bool configuration)
        {
            try
            {
                int config = configuration ? 1 : 0; // mimics win32 BOOL type.
                SetWindowFeedbackSetting(hwnd, feedback, 0, sizeof(int), &config);
            }
            catch
            {
                // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowfeedbacksetting#requirements
                // this API only exists in Win8+.
            }
        }
    }

    public enum FeedbackType
    {
        TouchContactVisualization = 1,
        PenBarrelVisualization = 2,
        PenTap = 3,
        PenDoubleTap = 4,
        PenPressAndHold = 5,
        PenRightTap = 6,
        TouchTap = 7,
        TouchDoubleTap = 8,
        TouchPressAndHold = 9,
        TouchRightTap = 10,
        GesturePressAndTap = 11,
    }
}
