// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osuTK;
using osuTK.Graphics.OpenGL;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSGameHost : DesktopGameHost
    {
        internal MacOSGameHost(string gameName, bool bindIPC = false, ToolkitOptions toolkitOptions = default, bool portableInstallation = false, bool useSdl = false)
            : base(gameName, bindIPC, toolkitOptions, portableInstallation, useSdl)
        {
        }

        protected override IWindow CreateWindow() =>
            !UseSdl ? (IWindow)new MacOSGameWindow() : new SDLWindow();

        protected override Storage GetStorage(string baseName) => new MacOSStorage(baseName, this);

        public override ITextInputSource GetTextInput() => Window == null ? null : new MacOSTextInput(Window);

        public override Clipboard GetClipboard() => new MacOSClipboard();

        protected override void Swap()
        {
            base.Swap();

            // It has been reported that this helps performance on macOS (https://github.com/ppy/osu/issues/7447)
            if (Window.VSync != VSyncMode.On)
                GL.Finish();
        }

        public override IEnumerable<KeyBinding> PlatformKeyBindings => new[]
        {
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.X), new PlatformAction(PlatformActionType.Cut)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.C), new PlatformAction(PlatformActionType.Copy)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.V), new PlatformAction(PlatformActionType.Paste)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.A), new PlatformAction(PlatformActionType.SelectAll)),
            new KeyBinding(InputKey.Left, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.Right, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.BackSpace, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(InputKey.Delete, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Left), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Right), new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.BackSpace), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Shift, InputKey.Delete), new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Left), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Right), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.BackSpace), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Delete), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Shift, InputKey.Left), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Shift, InputKey.Right), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Left), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Right), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.BackSpace), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Delete), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.Left), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.Right), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Super, InputKey.Left), new PlatformAction(PlatformActionType.DocumentPrevious)),
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.Super, InputKey.Right), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Tab), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Tab), new PlatformAction(PlatformActionType.DocumentPrevious)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.S), new PlatformAction(PlatformActionType.Save)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Up), new PlatformAction(PlatformActionType.ListStart, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Down), new PlatformAction(PlatformActionType.ListEnd, PlatformActionMethod.Move))
        };
    }
}
