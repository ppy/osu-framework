// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Foundation;
using UIKit;
using UniformTypeIdentifiers;

namespace osu.Framework.iOS
{
    internal class IOSFilePresenter : UIDocumentInteractionControllerDelegate
    {
        private readonly IOSWindow window;
        private readonly UIDocumentInteractionController viewController = new UIDocumentInteractionController();

        internal IOSFilePresenter(IOSWindow window)
        {
            this.window = window;
        }

        public bool OpenFile(string filename)
        {
            setupViewController(filename);

            if (viewController.PresentPreview(true))
                return true;

            var gameView = window.UIWindow.RootViewController!.View!;
            return viewController.PresentOpenInMenu(gameView.Bounds, gameView, true);
        }

        public bool PresentFile(string filename)
        {
            setupViewController(filename);

            var gameView = window.UIWindow.RootViewController!.View!;
            return viewController.PresentOptionsMenu(gameView.Bounds, gameView, true);
        }

        private void setupViewController(string filename)
        {
            var url = NSUrl.FromFilename(filename);

            viewController.Url = url;
            viewController.Delegate = this;

            if (OperatingSystem.IsIOSVersionAtLeast(14))
                viewController.Uti = UTType.CreateFromExtension(Path.GetExtension(filename))?.Identifier ?? UTTypes.Data.Identifier;
        }

        public override UIViewController ViewControllerForPreview(UIDocumentInteractionController controller) => window.UIWindow.RootViewController!;

        public override void WillBeginSendingToApplication(UIDocumentInteractionController controller, string? application)
        {
            // this path is triggered when a user opens the presented document in another application,
            // the menu does not dismiss afterward and locks the game indefinitely. dismiss it manually.
            viewController.DismissMenu(true);
        }
    }
}
