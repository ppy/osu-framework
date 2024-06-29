// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using osu.Framework.Android.Graphics.Textures;
using osu.Framework.Android.Graphics.Video;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;
using Uri = Android.Net.Uri;

namespace osu.Framework.Android
{
    public class AndroidGameHost : SDLGameHost
    {
        private readonly AndroidGameActivity activity;

        public AndroidGameHost(AndroidGameActivity activity)
            : base(string.Empty)
        {
            this.activity = activity;
        }

        protected override void SetupConfig(IDictionary<FrameworkSetting, object> defaultOverrides)
        {
            if (!defaultOverrides.ContainsKey(FrameworkSetting.ExecutionMode))
                defaultOverrides.Add(FrameworkSetting.ExecutionMode, ExecutionMode.SingleThread);

            base.SetupConfig(defaultOverrides);
        }

        protected override IWindow CreateWindow(GraphicsSurfaceType preferredSurface) => new AndroidGameWindow(preferredSurface, Options.FriendlyGameName);

        protected override void DrawFrame()
        {
            if (AndroidGameActivity.Surface.IsSurfaceReady)
                base.DrawFrame();
        }

        public override bool CanExit => false;

        public override bool CanSuspendToBackground => true;

        public override bool OnScreenKeyboardOverlapsGameWindow => true;

        public override string InitialFileSelectorPath => @"/sdcard";

        public override Storage GetStorage(string path) => new AndroidStorage(path, this);

        public override IEnumerable<string> UserStoragePaths
            // not null as internal "external storage" is always available.
            => Application.Context.GetExternalFilesDir(string.Empty).AsNonNull().ToString().Yield();

        public override bool OpenFileExternally(string filename) => false;

        public override bool PresentFileExternally(string filename) => false;

        public override void OpenUrlExternally(string url)
        {
            if (!url.CheckIsValidUrl())
                throw new ArgumentException("The provided URL must be one of either http://, https:// or mailto: protocols.", nameof(url));

            try
            {
                using (var intent = new Intent(Intent.ActionView, Uri.Parse(url)))
                {
                    // Recommended way to open URLs on Android 11+
                    // https://developer.android.com/training/package-visibility/use-cases#open-urls-browser-or-other-app
                    activity.StartActivity(intent);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to open external link.");
            }
        }

        public override IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new AndroidTextureLoaderStore(underlyingStore);

        public override VideoDecoder CreateVideoDecoder(Stream stream)
            => new AndroidVideoDecoder(Renderer, stream);

        public override bool SuspendToBackground()
        {
            return activity.MoveTaskToBack(true);
        }
    }
}
