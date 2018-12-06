// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Audio;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.IO.Stores;
using osu.Framework.iOS.Audio;
using osu.Framework.iOS.Input;
using osu.Framework.Platform;
using osu.Framework.Platform.MacOS;
using osu.Framework.Threading;

namespace osu.Framework.iOS
{
    // ReSharper disable once InconsistentNaming
    public class iOSGameHost : GameHost
    {
        private readonly iOSGameView gameView;

        public iOSGameHost(iOSGameView gameView)
        {
            this.gameView = gameView;
            iOSGameWindow.GameView = gameView;
            Window = new iOSGameWindow();
        }

        public override ITextInputSource GetTextInput() => new iOSTextInput(gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[] { new iOSTouchHandler(gameView), new iOSKeyboardHandler(gameView) };

        protected override Storage GetStorage(string baseName) => new MacOSStorage(baseName, this);

        public override void OpenFileExternally(string filename) => throw new NotImplementedException();

        public override void OpenUrlExternally(string url) => throw new NotImplementedException();

        public override AudioManager CreateAudioManager(ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore, Scheduler eventScheduler) =>
            new iOSAudioManager(trackStore, sampleStore) { EventScheduler = eventScheduler };
    }
}
