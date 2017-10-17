// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSGameHost : DesktopGameHost
    {
        internal MacOSGameHost(string gameName, bool bindIPC = false)
            : base(gameName, bindIPC)
        {
            Window = new MacOSGameWindow();
            Window.WindowStateChanged += (sender, e) =>
            {
                if (Window.WindowState != OpenTK.WindowState.Minimized)
                    OnActivated();
                else
                    OnDeactivated();
            };
        }

        protected override Storage GetStorage(string baseName) => new MacOSStorage(baseName);

        public override Clipboard GetClipboard() => new MacOSClipboard();

        public override IEnumerable<KeyBinding> PlatformKeyBindings => new[]
        {
            new KeyBinding(new KeyCombination(new[] { InputKey.Win, InputKey.X }), PlatformAction.Cut),
            new KeyBinding(new KeyCombination(new[] { InputKey.Win, InputKey.C }), PlatformAction.Copy),
            new KeyBinding(new KeyCombination(new[] { InputKey.Win, InputKey.V }), PlatformAction.Paste)
        };
    }
}
