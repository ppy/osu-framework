// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK.Input;

namespace osu.Framework.Platform.Windows
{
    internal class WindowsGameWindow : DesktopGameWindow
    {
        private const int seticon_message = 0x0080;

        protected override void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F4 && e.Alt)
            {
                Implementation.Exit();
                return;
            }

            base.OnKeyDown(sender, e);
        }

        public override Icon Icon
        {
            get => base.Icon;
            set
            {
                if (base.Icon == value)
                    return;

                base.Icon = value;

                if (WindowInfo.Handle == IntPtr.Zero)
                    throw new InvalidOperationException("Window must be created before an icon can be set.");

                SendMessage(WindowInfo.Handle, seticon_message, (IntPtr)0, base.Icon?.Handle ?? IntPtr.Zero);
                SendMessage(WindowInfo.Handle, seticon_message, (IntPtr)1, base.Icon?.Handle ?? IntPtr.Zero);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
