// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osuTK;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSGameHost : DesktopGameHost
    {
        internal MacOSGameHost(string gameName, bool bindIPC = false, ToolkitOptions toolkitOptions = default)
            : base(gameName, bindIPC, toolkitOptions)
        {
            Window = new MacOSGameWindow();
            Window.WindowStateChanged += (sender, e) =>
            {
                if (Window.WindowState != WindowState.Minimized)
                    OnActivated();
                else
                    OnDeactivated();
            };
        }

        protected override Storage GetStorage(string baseName) => new MacOSStorage(baseName, this);

        public override Clipboard GetClipboard() => new MacOSClipboard();

        public override IEnumerable<KeyBinding> PlatformKeyBindings => new[]
        {
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.X }), new PlatformAction(PlatformActionType.Cut)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.C }), new PlatformAction(PlatformActionType.Copy)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.V }), new PlatformAction(PlatformActionType.Paste)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.A }), new PlatformAction(PlatformActionType.SelectAll)),
            new KeyBinding(InputKey.Left, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.Right, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Move)),
            new KeyBinding(InputKey.BackSpace, new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(InputKey.Delete, new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Left }), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Right }), new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.BackSpace }), new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Shift, InputKey.Delete }), new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Left }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Right }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.BackSpace}), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Delete }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Shift, InputKey.Left }), new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Shift, InputKey.Right }), new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Left }), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Right }), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.BackSpace }), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Delete }), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Delete)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Shift, InputKey.Left }), new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Super, InputKey.Shift, InputKey.Right }), new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Select)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Super, InputKey.Left }), new PlatformAction(PlatformActionType.DocumentPrevious)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Alt, InputKey.Super, InputKey.Right }), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Tab }), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(new[] { InputKey.Control, InputKey.Shift, InputKey.Tab }), new PlatformAction(PlatformActionType.DocumentPrevious)),
        };
    }
}
