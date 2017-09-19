// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Windows.Forms;

namespace osu.Framework.Desktop.Platform.MacOS
{
    public class MacOSClipboard : Framework.Platform.Clipboard
    {
        public override string GetText()
        {
            return Clipboard.GetText(TextDataFormat.UnicodeText);
        }

        public override void SetText(string selectedText)
        {
            //Clipboard.SetText(selectedText);

            //This works within osu but will hang any application you try to paste to afterwards until osu is closed.
            //Likely requires the use of X libraries directly to fix
        }
    }
}
