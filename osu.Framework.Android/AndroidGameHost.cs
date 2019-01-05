// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Threading;
using Android.App;
using osu.Framework.Android.Graphics.Textures;
using osu.Framework.Android.Input;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Framework.Android
{
    public class AndroidGameHost : GameHost
    {
        private readonly AndroidGameView gameView;

        public AndroidGameHost(AndroidGameView gameView)
        {
            this.gameView = gameView;
            Window = new AndroidGameWindow(gameView);
        }

        public override ITextInputSource GetTextInput()
            => new AndroidTextInput(gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
            => new InputHandler[] { new AndroidKeyboardHandler(gameView), new AndroidTouchHandler(gameView) };

        protected override Storage GetStorage(string baseName)
            => new AndroidStorage(baseName, this);

        public override void OpenFileExternally(string filename)
            => throw new NotImplementedException();

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
