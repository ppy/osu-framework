// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Runtime.Versioning;
using Foundation;
using osu.Framework.Platform;
using UIKit;
using UniformTypeIdentifiers;

namespace osu.Framework.iOS
{
    [SupportedOSPlatform("ios14.0")]
    public class IOSFileSelector : UIDocumentPickerDelegate, ISystemFileSelector
    {
        public event Action<FileInfo>? Selected;

        private readonly IIOSWindow window;

        private readonly UIDocumentPickerViewController documentPicker;

        public IOSFileSelector(IIOSWindow window, string[] allowedExtensions)
        {
            this.window = window;

            UTType[] utTypes;

            if (allowedExtensions.Length == 0)
                utTypes = new[] { UTTypes.Data };
            else
            {
                utTypes = new UTType[allowedExtensions.Length];

                for (int i = 0; i < allowedExtensions.Length; i++)
                {
                    string extension = allowedExtensions[i];

                    var type = UTType.CreateFromExtension(extension.Replace(".", string.Empty));
                    if (type == null)
                        throw new InvalidOperationException($"System failed to recognise extension \"{extension}\" while preparing the file selector.\n");

                    utTypes[i] = type;
                }
            }

            // files must be provided as copies, as they may be originally located in places that cannot be accessed freely (aka. iCloud Drive).
            // we can acquire access to those files via startAccessingSecurityScopedResource but we must know when the game has finished using them.
            // todo: refactor FileSelector/DirectorySelector to be aware when the game finished using a file/directory.
            documentPicker = new UIDocumentPickerViewController(utTypes, true);
            documentPicker.Delegate = this;
        }

        public void Present()
        {
            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                window.ViewController.PresentViewController(documentPicker, true, null);
            });
        }

        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
            => Selected?.Invoke(new FileInfo(url.Path!));

        protected override void Dispose(bool disposing)
        {
            documentPicker.Dispose();
            base.Dispose(disposing);
        }
    }
}
