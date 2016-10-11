// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.Windows.Forms;
using osu.Framework.Desktop.Input;
using osu.Framework.Input;
using osu.Framework.Platform;
using OpenTK.Graphics;

namespace osu.Framework.Desktop.Platform
{
    public class DesktopGameWindow : BasicGameWindow
    {
        private const int default_width = 1366;
        private const int default_height = 768;

        public DesktopGameWindow() : base(default_width, default_height)
        {
            /*Form = CreateGameForm(flags);
            Form.StartPosition = FormStartPosition.CenterScreen;
            Form.ClientSize = new Size(default_width, default_height); // if no screen res is set the default will be used instead
            Form.ScreenChanged += delegate { OnScreenDeviceNameChanged(); };
            Form.Activated += delegate { OnActivated(); };
            Form.Deactivate += delegate { OnDeactivated(); };
            Form.SizeChanged += delegate { OnClientSizeChanged(); };
            Form.Paint += delegate { OnPaint(); };
            Form.FormClosing += Form_FormClosing;
            Form.FormClosed += delegate { OnExited(); };*/
        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = OnExitRequested();
        }

        internal TextInputSource CreateTextInput() => null;// new BackingTextBox(Form);
    }
}
