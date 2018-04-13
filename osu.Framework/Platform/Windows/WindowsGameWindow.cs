// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Input;

namespace osu.Framework.Platform.Windows
{
    internal class WindowsGameWindow : DesktopGameWindow
    {
        private const int seticon_message = 0x0080;

        private Icon smallIcon;
        private Icon largeIcon;

        protected override void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F4 && e.Alt)
            {
                Implementation.Exit();
                return;
            }

            base.OnKeyDown(sender, e);
        }

        public override void SetIconFromStream(Stream stream)
        {
            if (WindowInfo.Handle == IntPtr.Zero)
                throw new InvalidOperationException("Window must be created before an icon can be set.");

            var secondStream = new MemoryStream();
            stream.CopyTo(secondStream);

            stream.Position = 0;
            secondStream.Position = 0;

            smallIcon = new Icon(stream, 24, 24);
            largeIcon = new Icon(secondStream, 256, 256);

            SendMessage(WindowInfo.Handle, seticon_message, (IntPtr)0, smallIcon.Handle);
            SendMessage(WindowInfo.Handle, seticon_message, (IntPtr)1, largeIcon.Handle);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
