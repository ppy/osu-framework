// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using osuTK.Input;
using System.Linq;
using System.Drawing;
using osu.Framework.Logging;

namespace osu.Framework.Platform.Windows
{
    internal class WindowsGameWindow : DesktopGameWindow
    {
        private const int seticon_message = 0x0080;

        private object smallIcon;
        private object largeIcon;

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

            try
            {
                // get the type info with reflection, since Icon won't be available to Xamarin
                var drawingAssembly = typeof(Point).Assembly;
                Type iconType = drawingAssembly.ExportedTypes.Single(x => x.Name == "Icon");
                ConstructorInfo cons = iconType.GetConstructor(new Type[] { typeof(Stream), typeof(int), typeof(int) });
                PropertyInfo handleProp = iconType.GetProperties().Single(x => x.Name == "Handle");

                // create icons and get their handles
                smallIcon = cons.Invoke(new object[] { stream, 24, 24 });
                largeIcon = cons.Invoke(new object[] { secondStream, 256, 256 });
                IntPtr smallIconHandle = (IntPtr)handleProp.GetValue(smallIcon);
                IntPtr largeIconHandle = (IntPtr)handleProp.GetValue(largeIcon);

                // pass the handles through to SendMessage
                SendMessage(WindowInfo.Handle, seticon_message, (IntPtr)0, smallIconHandle);
                SendMessage(WindowInfo.Handle, seticon_message, (IntPtr)1, largeIconHandle);
            }
            catch
            {
                Logger.Log("Failed to set window icon from Stream.", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
