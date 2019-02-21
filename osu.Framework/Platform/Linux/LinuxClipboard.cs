// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// using System.Windows.Forms;

namespace osu.Framework.Platform.Linux
{
    public class LinuxClipboard : Clipboard
    {
        public override string GetText()
        {
            return string.Empty;
            // return System.Windows.Forms.Clipboard.GetText(TextDataFormat.UnicodeText);
        }

        public override void SetText(string selectedText)
        {
            //Clipboard.SetText(selectedText);

            //This works within osu but will hang any application you try to paste to afterwards until osu is closed.
            //Likely requires the use of X libraries directly to fix
        }
    }
}
