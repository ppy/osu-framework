// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Input;
using osu.Framework.Platform.Windows.Native;

namespace osu.Framework.Platform.Windows
{
    public class WindowsTextInput : GameWindowTextInput
    {
        public WindowsTextInput(IWindow window)
            : base(window)
        {
        }

        private void startTabTip() => Task.Run(() => Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), @"microsoft shared\ink\TabTip.exe"),
            UseShellExecute = true,
        }));

        private void closeTouchKeyboard()
        {
            var kbdWnd = Methods.FindWindow("IPTIP_Main_Window", null);
            Methods.PostMessage(kbdWnd, (uint)WindowsMessage.SYSCOMMAND, (IntPtr)SystemCommand.CLOSE, IntPtr.Zero);
        }

        public override void Activate(object sender)
        {
            // TabTip is clever enough to know when to show the touch keyboard so there's no need to add conditions here.
            startTabTip();

            base.Activate(sender);
        }

        public override void Deactivate(object sender)
        {
            closeTouchKeyboard();
            base.Deactivate(sender);
        }
    }
}
