//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Windows.Forms;
using osu.Framework.Desktop.OS.Windows;
using osu.Framework.Framework;
using osu.Framework.Input;

namespace osu.Framework.Desktop.Input
{
    public class BackingTextBox : ImeTextBox, TextInputSource
    {
        Form form;

        public BackingTextBox(BasicGameForm form)
        {
            this.form = form;

            Location = new System.Drawing.Point(-9999, -9999);

            OnNewImeComposition += Textbox_OnNewImeComposition;
            OnImeActivity += Textbox_OnImeActivity;
        }

        private void Textbox_OnImeActivity(bool b)
        {
            Debug.Print($@"IME: {b}");
        }

        private void Textbox_OnNewImeComposition(string s)
        {
            Debug.Print(s);
        }

        public void Activate()
        {
            Debug.Assert(!form.Controls.Contains(this));

            if (form.ActiveControl != null)
                form.ActiveControl.ImeMode = ImeMode.Off;

            form.Controls.Add(this);
            Focus();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            ImeMode = ImeMode.On;
        }

        public void Deactivate()
        {
            ImeMode = ImeMode.Off;
            form.Controls.Remove(this);
            if (form.Controls.Count > 0)
                form.Controls[form.Controls.Count - 1].Focus();
        }

        public string GetPendingText()
        {
            string pending = Text;
            Text = string.Empty;
            return pending;
        }
    }
}
