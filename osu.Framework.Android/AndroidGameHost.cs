// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using osu.Framework.Android.Graphics.Textures;
using osu.Framework.Android.Graphics.Video;
using osu.Framework.Android.Input;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using Uri = Android.Net.Uri;

namespace osu.Framework.Android
{
    public class AndroidGameHost : OsuTKGameHost
    {
        private readonly AndroidGameView gameView;

        public AndroidGameHost(AndroidGameView gameView)
        {
            this.gameView = gameView;
        }

        protected override void SetupConfig(IDictionary<FrameworkSetting, object> defaultOverrides)
        {
            if (!defaultOverrides.ContainsKey(FrameworkSetting.ExecutionMode))
                defaultOverrides.Add(FrameworkSetting.ExecutionMode, ExecutionMode.SingleThread);

            base.SetupConfig(defaultOverrides);
        }

        protected override IWindow CreateWindow() => new AndroidGameWindow(gameView);

        protected override bool LimitedMemoryEnvironment => true;

        public override bool CanExit => false;

        public override bool OnScreenKeyboardOverlapsGameWindow => true;

        protected override TextInputSource CreateTextInput() => new AndroidTextInput(gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[]
            {
                new AndroidMouseHandler(gameView),
                new AndroidKeyboardHandler(gameView),
                new AndroidTouchHandler(gameView),
                new MidiHandler()
            };

        public override string InitialFileSelectorPath => @"/sdcard";

        public override Storage GetStorage(string path) => new AndroidStorage(path, this);

        public override IEnumerable<string> UserStoragePaths => new[]
        {
            // not null as internal "external storage" is always available.
            Application.Context.GetExternalFilesDir(string.Empty)!.ToString(),
        };

        public override void OpenFileExternally(string filename)
            => throw new NotImplementedException();

        public override void PresentFileExternally(string filename)
            => throw new NotImplementedException();

        public override void OpenUrlExternally(string url)
        {
            var activity = (Activity)gameView.Context;

            if (activity?.PackageManager == null) return;

            using (var intent = new Intent(Intent.ActionView, Uri.Parse(url)))
            {
                if (intent.ResolveActivity(activity.PackageManager) != null)
                    activity.StartActivity(intent);
            }
        }

        public override IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new AndroidTextureLoaderStore(underlyingStore);

        public override VideoDecoder CreateVideoDecoder(Stream stream)
            => new AndroidVideoDecoder(stream);
    }
}
