﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using osu.Framework.Android.Backends;
using osu.Framework.Android.Graphics.Textures;
using osu.Framework.Android.Input;
using osu.Framework.Backends;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using Uri = Android.Net.Uri;

namespace osu.Framework.Android
{
    public class AndroidGameHost : GameHost
    {
        private readonly AndroidGameView gameView;

        public AndroidGameHost(AndroidGameView gameView, IBackendProvider backends = null)
            : base(backends: backends ?? new AndroidBackendProvider())
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
            => throw new NotImplementedException();

        public override void OpenUrlExternally(string url)
        {
            var activity = (Activity)gameView.Context;

            using (var intent = new Intent(Intent.ActionView, Uri.Parse(url)))
            {
                if (intent.ResolveActivity(activity.PackageManager) != null)
                    activity.StartActivity(intent);
            }
        }

        public override IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new AndroidTextureLoaderStore(underlyingStore);

        protected override void PerformExit(bool immediately)
        {
            // Do not exit on Android, Window.Run() does not block
        }
    }
}
