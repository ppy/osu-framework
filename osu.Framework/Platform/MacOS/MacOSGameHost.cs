// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Mouse;
using osuTK;
using osuTK.Graphics.OpenGL;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSGameHost : DesktopGameHost
    {
        internal MacOSGameHost(string gameName, bool bindIPC = false, ToolkitOptions toolkitOptions = default, bool portableInstallation = false)
            : base(gameName, bindIPC, portableInstallation)
        {
        }

        protected override IWindow CreateWindow() => new MacOSWindow();

        public override string UserStoragePath
        {
            get
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                string[] paths =
                {
                    xdg ?? Path.Combine(home, ".local", "share"),
                    Path.Combine(home)
                };

                foreach (string path in paths)
                {
                    if (Directory.Exists(path))
                        return path;
                }

                return paths[0];
            }
        }

        public override ITextInputSource GetTextInput() => Window == null ? null : new MacOSTextInput(Window);

        public override Clipboard GetClipboard() => new MacOSClipboard();

        protected override void Swap()
        {
            base.Swap();

            // It has been reported that this helps performance on macOS (https://github.com/ppy/osu/issues/7447)
            if (!Window.VerticalSync)
                GL.Finish();
        }

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
        {
            var handlers = base.CreateAvailableInputHandlers();

            foreach (var h in handlers.OfType<MouseHandler>())
            {
                // There are several bugs we need to fix with macOS / SDL2 cursor handling before switching this on.
                h.UseRelativeMode.Value = false;
            }

            return handlers;
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
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.W), new PlatformAction(PlatformActionType.DocumentClose)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.T), new PlatformAction(PlatformActionType.TabNew)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.T), new PlatformAction(PlatformActionType.TabRestore)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Tab), new PlatformAction(PlatformActionType.DocumentNext)),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Tab), new PlatformAction(PlatformActionType.DocumentPrevious)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.S), new PlatformAction(PlatformActionType.Save)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Up), new PlatformAction(PlatformActionType.ListStart, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Down), new PlatformAction(PlatformActionType.ListEnd, PlatformActionMethod.Move)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Z), new PlatformAction(PlatformActionType.Undo)),
            new KeyBinding(new KeyCombination(InputKey.Super, InputKey.Shift, InputKey.Z), new PlatformAction(PlatformActionType.Redo)),
        };
    }
}
