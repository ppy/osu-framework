// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Platform;
using SixLabors.ImageSharp;
using UIKit;

namespace osu.Framework.iOS
{
    // todo: check whether invoking on main thread is required, and whether we could just replace this with SDL clipboard implementation.
    public class IOSClipboard : Clipboard
    {
        internal IOSClipboard()
        {
        }

        public override string GetText()
        {
            string text = "";
            text = UIPasteboard.General.String;
            return text;
        }

        public override void SetText(string selectedText) => UIPasteboard.General.String = selectedText;

        public override Image<TPixel> GetImage<TPixel>()
        {
            return null;
        }

        public override bool SetImage(Image image)
        {
            return false;
        }
    }
}
