// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using Android.App;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Android
{
    public class AndroidGameHost : GameHost
    {
        private readonly AndroidGameView gameView;

        public AndroidGameHost(AndroidGameView gameView)
        {
            this.gameView = gameView;
            AndroidGameWindow.view = gameView;
            Window = new AndroidGameWindow();
            Window.WindowStateChanged += (sender, e) =>
            {
                if (Window.WindowState != WindowState.Minimized)
                    OnActivated();
                else
                    OnDeactivated();
            };
        }
        /*protected override void UpdateInitialize()
        {
            Activity activity = (Activity)gameView.Context;
            activity.RunOnUiThread(() =>
            {
                base.UpdateInitialize();
            });
        }
        protected override void UpdateFrame()
        {
            Activity activity = (Activity)gameView.Context;
            activity.RunOnUiThread(() =>
            {
                base.UpdateFrame();
            });
        }

        protected override void DrawInitialize()
        {
            Activity activity = (Activity)gameView.Context;
            activity.RunOnUiThread(() =>
            {
                base.DrawInitialize();
            });
        }

        protected override void DrawFrame()
        {
            Activity activity = (Activity)gameView.Context;
            activity.RunOnUiThread(() =>
            {
                base.DrawFrame();
            });
        }*/

        public override ITextInputSource GetTextInput() => throw new NotImplementedException();// new AndroidTextInput(gameView);

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[] { };//new AndroidTouchHandler(gameView), new AndroidKeyboardHandler(gameView) };

        protected override Storage GetStorage(string baseName) => new AndroidStorage(baseName, this);

        public override void OpenFileExternally(string filename) => throw new NotImplementedException();

        public override void OpenUrlExternally(string url) => throw new NotImplementedException();
    }
}
