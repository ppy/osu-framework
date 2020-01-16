// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Platform.Windows.Native;
using osuTK.Input;

namespace osu.Framework.Platform.Windows
{
    internal class WindowsGameWindow : DesktopGameWindow
    {
        private IconGroup iconGroup;
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

            iconGroup = new IconGroup(stream);

            smallIcon = iconGroup.CreateIcon(24, 24);
            largeIcon = iconGroup.CreateIcon(256, 256);

            Methods.SendMessage(WindowInfo.Handle, (int)WindowsMessage.SETICON, (IntPtr)0, smallIcon.Handle);
            Methods.SendMessage(WindowInfo.Handle, (int)WindowsMessage.SETICON, (IntPtr)1, largeIcon.Handle);
        }
    }
}
