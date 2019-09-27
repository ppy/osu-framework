// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.Content.PM;
using osu.Framework.Android.Graphics.Textures;
using osu.Framework.Android.Input;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using Xamarin.Essentials;
using AndroidUri = Android.Net.Uri;

namespace osu.Framework.Android
{
    public class AndroidGameHost : GameHost
    {
        private readonly AndroidGameView gameView;

        public AndroidGameHost(AndroidGameView gameView)
        {
            this.gameView = gameView;
        }

        protected override void SetupForRun()
        {
            base.SetupForRun();
            AndroidGameWindow.View = gameView;
            Window = new AndroidGameWindow();
        }

        protected override bool LimitedMemoryEnvironment => true;

        public override bool CanExit => false;

        public override bool OnScreenKeyboardOverlapsGameWindow => true;

        public override ITextInputSource GetTextInput()
            => new AndroidTextInput(gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
            => new InputHandler[] { new AndroidKeyboardHandler(gameView), new AndroidTouchHandler(gameView) };

        protected override Storage GetStorage(string baseName)
            => new AndroidStorage(baseName, this);

        public override void OpenFileExternally(string filename)
        {
            if (Directory.Exists(filename))
            {
                openFolderExternally(filename);
                return;
            }
            Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filename)
            });
        }

        private void openFolderExternally(string filename)
        {
            Context context = Application.Context.ApplicationContext;
            PackageManager pm = context.PackageManager;
            var contentUri = AndroidUri.Parse(filename);

            Intent intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(contentUri, "resource/folder");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            if (intent.ResolveActivity(pm) == null)
                return;
            var chooserIntent = Intent.CreateChooser(intent, "Choose your file manager");
            chooserIntent.SetFlags(ActivityFlags.ClearTop);
            chooserIntent.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(chooserIntent);
        }

        public override void OpenUrlExternally(string url)
            => throw new NotImplementedException();

        public override IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new AndroidTextureLoaderStore(underlyingStore);

        protected override void PerformExit(bool immediately)
        {
            // Do not exit on Android, Window.Run() does not block
        }
    }
}
