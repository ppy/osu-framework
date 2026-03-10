// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using AndroidX.Core.Content;
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
using Stream = System.IO.Stream;
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

        public override Storage GetStorage(string path) => new AndroidStorage(path, this);

        public override IEnumerable<string> UserStoragePaths
            // not null as internal "external storage" is always available.
            => Application.Context.GetExternalFilesDir(string.Empty).AsNonNull().ToString().Yield();

        public override ISystemFileSelector CreateSystemFileSelector(string[] allowedExtensions)
            => new AndroidFileSelector(activity, allowedExtensions);

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// On Android, this method and <see cref="PresentFileExternally"/> have the same behaviour.
        /// </para>
        /// <para>
        /// Because of Android's stringent restrictions on accessing files on the device,
        /// this method will pretty much only work on files that the game directly controls or creates in its dedicated storages,
        /// and even then, only if they are explicitly allowlisted as accessible via a <c>FileProvider</c>.
        /// See provided example below for how to set up sharing.
        /// </para>
        /// <para>
        /// If this method is prompted to open a file that is not in this game's storages, or the file path is not whitelisted, this method will return <see langword="false"/>
        /// and log the appropriate error.
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>
        /// In <c>AndroidManifest.xml</c>:
        /// <code>
        /// &lt;manifest xmlns:android="http://schemas.android.com/apk/res/android" package="com.example.MyGame" android:installLocation="auto"&gt;
        ///     &lt;application android:label="osu!framework test"&gt;
        ///          &lt;provider android:name="androidx.core.content.FileProvider"
        ///                    android:authorities="com.example.MyGame.fileprovider"
        ///                    android:grantUriPermissions="true"
        ///                    android:exported="false"&gt;
        ///               &lt;meta-data android:name="android.support.FILE_PROVIDER_PATHS"
        ///                          android:resource="@xml/filepaths" /&gt;
        ///          &lt;/provider&gt;
        ///     &lt;/application&gt;
        /// &lt;/manifest&gt;
        /// </code>
        /// Note that the authority of the file provider MUST be the package name suffixed with <c>.fileprovider</c>.
        /// </para>
        /// <para>
        /// In <c>Resources/xml/filepaths.xml</c>:
        /// <code>
        /// &lt;?xml version="1.0" encoding="utf-8"?&gt;
        /// &lt;paths&gt;
        ///      &lt;external-files-path path="logs" name="logs" /&gt;
        /// &lt;/paths&gt;
        /// </code>
        /// Paths in <c>&lt;external-files-path&gt;</c> tags are relative to the only path in <see cref="UserStoragePaths"/>.
        /// </para>
        /// </example>
        public override bool OpenFileExternally(string filename)
        {
            var context = activity.ApplicationContext!;
            Java.IO.File file = new Java.IO.File(filename);
            Uri? contentUri;

            try
            {
                contentUri = FileProvider.GetUriForFile(context, $"{context.PackageName}.fileprovider", file);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to create content URI for file: {filename}.\nError: {ex}");
                return false;
            }

            if (contentUri == null)
                return false;

            // https://developer.android.com/training/sharing/send#send-binary-content
            // https://developer.android.com/reference/android/content/Intent#ACTION_SEND
            var shareIntent = new Intent(Intent.ActionSend);
            shareIntent.PutExtra(Intent.ExtraStream, contentUri);
            shareIntent.SetType(activity.ContentResolver?.GetType(contentUri));
            activity.StartActivity(Intent.CreateChooser(shareIntent, "Share"));
            return true;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// On Android, this method and <see cref="OpenFileExternally"/> have the same behaviour.
        /// See remarks of that method for instructions how to set up file sharing on Android.
        /// </remarks>
        public override bool PresentFileExternally(string filename) => OpenFileExternally(filename);

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
