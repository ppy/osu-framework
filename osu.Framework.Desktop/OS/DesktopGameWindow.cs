//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using osu.Framework.Desktop.Input;
using osu.Framework.Desktop.OS.Windows;
using osu.Framework.Input;
using osu.Framework.OS;
using OpenTK.Graphics;

namespace osu.Framework.Desktop.OS
{
    public abstract class DesktopGameWindow : BasicGameWindow
    {
        public override Rectangle ClientBounds => Form.ClientBounds;
        public override bool IsMinimized => Form.IsMinimized;
        public override IntPtr Handle => Form.Handle;

        private const int default_width = 1366;
        private const int default_height = 768;

        public override Size Size
        {
            get
            {
                return Form.ClientSize;
            }

            set
            {
                Form.ClientSize = value;
            }
        }

        protected abstract BasicGameForm CreateGameForm(GraphicsContextFlags flags);

        internal DesktopGameWindow(GraphicsContextFlags flags)
        {
            Form = CreateGameForm(flags);
            Form.ClientSize = new Size(default_width, default_height); // if no screen res is set the default will be used instead
            Form.ScreenChanged += delegate { OnScreenDeviceNameChanged(); };
            Form.ApplicationActivated += delegate { OnActivated(); };
            Form.ApplicationDeactivated += delegate { OnDeactivated(); };
            Form.SizeChanged += delegate { OnClientSizeChanged(); };
            Form.Paint += delegate { OnPaint(); };
            Form.FormClosing += Form_FormClosing;
            Form.FormClosed += delegate { OnExited(); };
        }

        private void Form_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            e.Cancel = OnExitRequested();
        }

        public override void Close()
        {
            Form.Close();
        }

        protected override void SetTitle(string title)
        {
            Form.Text = title;
        }

        internal TextInputSource CreateTextInput() => new BackingTextBox(Form);
    }
}
