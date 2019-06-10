// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.IO.Stores;
using osu.Framework.iOS.Graphics.Textures;
using osu.Framework.iOS.Input;
using osu.Framework.Platform;

namespace osu.Framework.iOS
{
    public class IOSGameHost : GameHost
    {
        private readonly IOSGameView gameView;

        public IOSGameHost(IOSGameView gameView)
        {
            this.gameView = gameView;
        }

        protected override void SetupForRun()
        {
            base.SetupForRun();
            IOSGameWindow.GameView = gameView;
            Window = new IOSGameWindow();
        }

        protected override void SetupConfig(IDictionary<FrameworkSetting, object> gameDefaults)
        {
            base.SetupConfig(gameDefaults);

            DebugConfig.Set(DebugSetting.BypassFrontToBackPass, true);
        }

        protected override void PerformExit(bool immediately)
        {
            // we shouldn't exit on iOS, as Window.Run does not block
        }

        public override bool OnScreenKeyboardOverlapsGameWindow => true;

        public override bool CanExit => false;

        public override ITextInputSource GetTextInput() => new IOSTextInput(gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[] { new IOSTouchHandler(gameView), new IOSKeyboardHandler(gameView), new IOSRawKeyboardHandler() };

        protected override Storage GetStorage(string baseName) => new IOSStorage(baseName, this);

        public override void OpenFileExternally(string filename) => throw new NotImplementedException();

        public override void OpenUrlExternally(string url) => throw new NotImplementedException();

        public override IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new IOSTextureLoaderStore(underlyingStore);
    }
}
