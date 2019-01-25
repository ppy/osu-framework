// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform.Windows.Native;
using osuTK;

namespace osu.Framework.Platform.Windows
{
    public class WindowsGameHost : DesktopGameHost
    {
        private readonly TimePeriod timePeriod;

        public override Clipboard GetClipboard() => new WindowsClipboard();

        protected override Storage GetStorage(string baseName) => new WindowsStorage(baseName, this);

        public override bool CapsLockEnabled => Console.CapsLock;

        internal WindowsGameHost(string gameName, bool bindIPC = false, ToolkitOptions toolkitOptions = default)
            : base(gameName, bindIPC, toolkitOptions)
        {
            // OnActivate / OnDeactivate may not fire, so the initial activity state may be unknown here.
            // In order to be certain we have the correct activity state we are querying the Windows API here.

            timePeriod = new TimePeriod(1) { Active = true };

            Window = new WindowsGameWindow();
            Window.WindowStateChanged += onWindowOnWindowStateChanged;
        }

        private void onWindowOnWindowStateChanged(object sender, EventArgs e)
        {
            if (Window.WindowState != WindowState.Minimized)
                OnActivated();
            else
                OnDeactivated();
        }

        protected override void Dispose(bool isDisposing)
        {
            Window.WindowStateChanged -= onWindowOnWindowStateChanged;

            timePeriod?.Dispose();
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
