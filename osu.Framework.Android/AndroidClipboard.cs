// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Android.Content;
using NuGet.Packaging;
using osu.Framework.Platform;
using SixLabors.ImageSharp;

namespace osu.Framework.Android
{
    public class AndroidClipboard : Clipboard
    {
        private readonly ClipboardManager? clipboardManager;
        private readonly Dictionary<string, string> customFormatValues = new Dictionary<string, string>();

        public AndroidClipboard(AndroidGameView view)
        {
            clipboardManager = view.Activity.GetSystemService(Context.ClipboardService) as ClipboardManager;
        }

        public override string? GetText() => clipboardManager?.PrimaryClip?.GetItemAt(0)?.Text;

        public override Image<TPixel>? GetImage<TPixel>() => null;

        public override bool SetData(ClipboardData data)
        {
            if (clipboardManager == null)
                return false;

            customFormatValues.Clear();
            clipboardManager.PrimaryClip = null;

            if (data.IsEmpty())
                return false;

            bool success = true;

            if (data.Text != null)
                clipboardManager.PrimaryClip = ClipData.NewPlainText(null, data.Text);

            if (data.Image != null)
                success = false;

            customFormatValues.AddRange(data.CustomFormatValues);

            return success;
        }

        public override string? GetCustom(string format)
        {
            return customFormatValues[format];
        }
    }
}
