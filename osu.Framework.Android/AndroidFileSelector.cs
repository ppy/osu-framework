// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Webkit;
using osu.Framework.Platform;

namespace osu.Framework.Android
{
    public class AndroidFileSelector : ISystemFileSelector
    {
        private readonly AndroidGameActivity activity;
        private readonly string[] allowedExtensions;
        public event Action<FileInfo>? Selected;

        public AndroidFileSelector(AndroidGameActivity activity, string[] allowedExtensions)
        {
            this.activity = activity;
            this.allowedExtensions = allowedExtensions;
        }

        public void Present()
        {
            activity.ActivityResultReceived += resultReceived;

            // https://developer.android.com/reference/android/content/Intent#ACTION_OPEN_DOCUMENT
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");

            foreach (string extension in allowedExtensions)
            {
                string? mimeType = MimeTypeMap.Singleton?.GetMimeTypeFromExtension(extension);
                if (mimeType != null)
                    intent.PutExtra(Intent.ExtraMimeTypes, mimeType);
            }

            activity.StartActivityForResult(intent, AndroidGameActivity.OPEN_DOCUMENT);
        }

        private void resultReceived(int requestCode, Result resultCode, Intent? data)
        {
            if (requestCode != AndroidGameActivity.OPEN_DOCUMENT)
                return;

            if (data?.Data == null)
                return;

            var tempFile = activity.CreateTemporaryFileFromContentUri(data.Data);
            Selected?.Invoke(tempFile);
        }

        public void Dispose()
        {
            activity.ActivityResultReceived -= resultReceived;
        }
    }
}
