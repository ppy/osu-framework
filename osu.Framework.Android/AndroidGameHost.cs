// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using osu.Framework.Platform.Linux;

namespace osu.Framework.Android
{
    public class AndroidGameHost : GameHost
    {
        //private readonly AndroidGameView gameView;

        public AndroidGameHost(AndroidGameView gameView)
        {
            //this.gameView = gameView;
            AndroidGameWindow.GameView = gameView;
            Window = new AndroidGameWindow();
        }

        public override ITextInputSource GetTextInput() => throw new NotImplementedException(); // new AndroidTextInput(gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[] { }; //new AndroidTouchHandler(gameView), new AndroidKeyboardHandler(gameView) };

        protected override Storage GetStorage(string baseName) => new LinuxStorage(baseName, this);

        public override void OpenFileExternally(string filename) => throw new NotImplementedException();

        public override void OpenUrlExternally(string url) => throw new NotImplementedException();
    }
}
