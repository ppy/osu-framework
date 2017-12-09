using System;
using System.Collections.Generic;
using OpenTK.Platform.iPhoneOS;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using osu.Framework.Platform.Windows;

namespace osu.Framework.Platform.iOS
{
    public class iOSGameHost : GameHost
    {
        public iOSGameHost(iPhoneOSGameView gameView)
        {
            Window = new iOSGameWindow(gameView);
        }

        public override ITextInputSource GetTextInput() => null;

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
        {
            yield break;
        }

        protected override Storage GetStorage(string baseName) => new WindowsStorage(baseName);
    }
}
