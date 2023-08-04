// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Content;
using osu.Framework.Platform;
using SixLabors.ImageSharp;

namespace osu.Framework.Android
{
    public class AndroidClipboard : Clipboard
    {
        private readonly ClipboardManager? clipboardManager;

        public AndroidClipboard(AndroidGameView view)
        {
            clipboardManager = view.Activity.GetSystemService(Context.ClipboardService) as ClipboardManager;
        }

        public override string? GetText() => clipboardManager?.PrimaryClip?.GetItemAt(0)?.Text;

        public override void SetText(string text)
        {
            if (clipboardManager != null)
                clipboardManager.PrimaryClip = ClipData.NewPlainText(null, text);
        }

        public override Image<TPixel>? GetImage<TPixel>() => null;

        public override bool SetImage(Image image) => false;
    }
}
