// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Platform.Windows.Native;

namespace osu.Framework.Platform.Windows
{
    public class WindowsGameHost : DesktopGameHost
    {
        private TimePeriod timePeriod;

        public override Clipboard GetClipboard() => new WindowsClipboard();

        protected override ReadableKeyCombinationProvider CreateReadableKeyCombinationProvider() => new WindowsReadableKeyCombinationProvider();

        public override IEnumerable<string> UserStoragePaths =>
            // on windows this is guaranteed to exist (and be usable) so don't fallback to the base/default.
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Yield();

#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        public override bool CapsLockEnabled => Console.CapsLock;

        internal WindowsGameHost(string gameName, HostOptions options)
            : base(gameName, options)
        {
        }

        public override bool OpenFileExternally(string filename)
        {
            if (Directory.Exists(filename))
            {
                // ensure the path always has one trailing DirectorySeparator so the native function opens the expected folder.
                string folder = filename.TrimDirectorySeparator() + Path.DirectorySeparatorChar;

                Explorer.OpenFolderAndSelectItem(folder);
                return true;
            }

            return base.OpenFileExternally(filename);
        }

        public override bool PresentFileExternally(string filename)
        {
            Explorer.OpenFolderAndSelectItem(filename.TrimDirectorySeparator());
            return true;
        }

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
        {
            // for windows platforms we want to override the relative mouse event handling behaviour.
            return base.CreateAvailableInputHandlers()
                       .Where(t => !(t is MouseHandler))
                       .Concat(new InputHandler[] { new WindowsMouseHandler() });
        }

        protected override void SetupForRun()
        {
            base.SetupForRun();

            // OnActivate / OnDeactivate may not fire, so the initial activity state may be unknown here.
            // In order to be certain we have the correct activity state we are querying the Windows API here.

            timePeriod = new TimePeriod(1);
        }

        protected override IWindow CreateWindow() => new WindowsWindow();

        public override IEnumerable<KeyBinding> PlatformKeyBindings => base.PlatformKeyBindings.Concat(new[]
        {
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.F4), PlatformAction.Exit)
        }).ToList();

        protected override void Dispose(bool isDisposing)
        {
            timePeriod?.Dispose();
            base.Dispose(isDisposing);
        }

        protected override void OnActivated()
        {
            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous | Execution.ExecutionState.SystemRequired | Execution.ExecutionState.DisplayRequired);
            base.OnActivated();
        }

        protected override void OnDeactivated()
        {
            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous);
            base.OnDeactivated();
        }
    }
}
