// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Desktop.Input.Handlers.Keyboard;
using osu.Framework.Desktop.Input.Handlers.Mouse;
using osu.Framework.Desktop.Platform.Windows.Native;
using osu.Framework.Input.Handlers;
using OpenTK.Graphics;
using osu.Framework.Platform;

namespace osu.Framework.Desktop.Platform.Windows
{
    public class WindowsGameHost : DesktopGameHost
    {
        private TimePeriod timePeriod;

        public override Clipboard GetClipboard() => new WindowsClipboard();

        internal WindowsGameHost(GraphicsContextFlags flags, string gameName, bool bindIPC = false) : base(gameName, bindIPC)
        {
            // OnActivate / OnDeactivate may not fire, so the initial activity state may be unknown here.
            // In order to be certain we have the correct activity state we are querying the Windows API here.

            timePeriod = new TimePeriod(1) { Active = true };

            Architecture.SetIncludePath();

            Window = new WindowsGameWindow();
            Window.WindowStateChanged += (sender, e) =>
            {
                if (Window.WindowState != OpenTK.WindowState.Minimized)
                    OnActivated();
                else
                    OnDeactivated();
            };

            Dependencies.Cache(Storage = new WindowsStorage(gameName));
        }

        public override IEnumerable<InputHandler> GetInputHandlers() => new InputHandler[] { new OpenTKMouseHandler(), new OpenTKKeyboardHandler() };

        protected override void Dispose(bool isDisposing)
        {
            timePeriod.Dispose();
            base.Dispose(isDisposing);
        }

        protected override void OnActivated()
        {
            timePeriod.Active = true;

            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous | Execution.ExecutionState.SystemRequired | Execution.ExecutionState.DisplayRequired);
            base.OnActivated();
        }

        protected override void OnDeactivated()
        {
            timePeriod.Active = false;

            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous);
            base.OnDeactivated();
        }
    }
}
