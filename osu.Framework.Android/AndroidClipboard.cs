// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Android.Content;
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

        public override bool SetData(params ClipboardEntry[] entries)
        {
            if (clipboardManager == null)
                return false;

            customFormatValues.Clear();
            clipboardManager.PrimaryClip = null;

            if (entries.Length == 0)
                return false;

            bool success = true;

            foreach (var entry in entries)
            {
                switch (entry)
                {
                    case ClipboardTextEntry textEntry:
                        clipboardManager.PrimaryClip = ClipData.NewPlainText(null, textEntry.Value);
                        break;

                    case ClipboardCustomEntry customEntry:
                        customFormatValues[customEntry.Format] = customFormatValues[customEntry.Value];
                        break;

                    default:
                        success = false;
                        break;
                }
            }

            return success;
        }

        public override string? GetCustom(string format)
        {
            return customFormatValues[format];
        }
    }
}
