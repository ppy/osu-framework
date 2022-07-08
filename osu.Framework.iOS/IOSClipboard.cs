// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Platform;
using SixLabors.ImageSharp;
using UIKit;

namespace osu.Framework.iOS
{
    public class IOSClipboard : Clipboard
    {
        private readonly IOSGameView gameView;

        internal IOSClipboard(IOSGameView gameView)
        {
            this.gameView = gameView;
        }

        public override string GetText()
        {
            string text = "";
            gameView.InvokeOnMainThread(() => text = UIPasteboard.General.String);
            return text;
        }

        public override void SetText(string selectedText) => gameView.InvokeOnMainThread(() => UIPasteboard.General.String = selectedText);

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
