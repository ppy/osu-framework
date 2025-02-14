// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Foundation;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.IO.Stores;
using osu.Framework.iOS.Graphics.Textures;
using osu.Framework.iOS.Graphics.Video;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Platform.MacOS;
using UIKit;

namespace osu.Framework.iOS
{
    public class IOSGameHost : SDLGameHost
    {
        public new IIOSWindow Window => (IIOSWindow)base.Window;

        private IOSFilePresenter presenter = null!;

        public override bool OnScreenKeyboardOverlapsGameWindow => true;

        public IOSGameHost()
            : base(string.Empty)
        {
        }

        protected override IWindow CreateWindow(GraphicsSurfaceType preferredSurface)
        {
            var window = new IOSWindow(preferredSurface, Options.FriendlyGameName);
            presenter = new IOSFilePresenter(window);
            return window;
        }

        protected override void SetupConfig(IDictionary<FrameworkSetting, object> defaultOverrides)
        {
            if (!defaultOverrides.ContainsKey(FrameworkSetting.ExecutionMode))
                defaultOverrides.Add(FrameworkSetting.ExecutionMode, ExecutionMode.SingleThread);

            base.SetupConfig(defaultOverrides);
        }

        public override bool CanExit => false;

        public override Storage GetStorage(string path) => new IOSStorage(path, this);

        public override bool OpenFileExternally(string filename)
        {
            UIApplication.SharedApplication.InvokeOnMainThread(() => presenter.OpenFile(filename));
            return true;
        }

        public override bool PresentFileExternally(string filename)
        {
            UIApplication.SharedApplication.InvokeOnMainThread(() => presenter.PresentFile(filename));
            return true;
        }

        public override void OpenUrlExternally(string url)
        {
            if (!url.CheckIsValidUrl()
                // App store links
                && !url.StartsWith("itms-apps://", StringComparison.Ordinal)
                // Testflight links
                && !url.StartsWith("itms-beta://", StringComparison.Ordinal))
                throw new ArgumentException("The provided URL must be one of either http://, https:// or mailto: protocols.", nameof(url));

            try
            {
                UIApplication.SharedApplication.InvokeOnMainThread(() =>
                {
                    NSUrl nsurl = NSUrl.FromString(url).AsNonNull();
                    if (UIApplication.SharedApplication.CanOpenUrl(nsurl))
                        UIApplication.SharedApplication.OpenUrl(nsurl, new NSDictionary(), null);
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to open external link.");
            }
        }

        public override IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new IOSTextureLoaderStore(underlyingStore);

        public override VideoDecoder CreateVideoDecoder(Stream stream)
            => new IOSVideoDecoder(Renderer, stream);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
        {
            var handlers = base.CreateAvailableInputHandlers();

            foreach (var h in handlers.OfType<MouseHandler>())
            {
                // Similar to macOS, "relative mode" is also broken on iOS.
                h.UseRelativeMode.Value = false;
                h.UseRelativeMode.Default = false;
            }

            return handlers;
        }

        public override ISystemFileSelector? CreateSystemFileSelector(string[] allowedExtensions)
        {
            IOSFileSelector? selector = null;

            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                // creating UIDocumentPickerViewController programmatically is only supported on iOS 14.0+.
                // on lower versions, return null and fall back to our normal file selector display.
                if (!OperatingSystem.IsIOSVersionAtLeast(14))
                    return;

                selector = new IOSFileSelector(Window, allowedExtensions);
            });

            return selector;
        }

        public override IEnumerable<KeyBinding> PlatformKeyBindings => MacOSGameHost.KeyBindings;
    }
}
