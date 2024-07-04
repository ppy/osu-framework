// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Platform.Windows.Native;

namespace osu.Framework.Platform.Windows
{
    [SupportedOSPlatform("windows")]
    public class WindowsGameHost : DesktopGameHost
    {
        private TimePeriod? timePeriod;

        protected override Clipboard CreateClipboard() => new WindowsClipboard();

        protected override ReadableKeyCombinationProvider CreateReadableKeyCombinationProvider() => new WindowsReadableKeyCombinationProvider();

        public override IEnumerable<string> UserStoragePaths
            // The base implementation returns %LOCALAPPDATA%, but %APPDATA% is a better default on Windows.
            => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create).Yield();

        public override bool CapsLockEnabled => Console.CapsLock;

        internal WindowsGameHost(string gameName, HostOptions? options)
            : base(gameName, options)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
            }
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

        protected override IRenderer CreateGLRenderer() => new WindowsGLRenderer(this);

        protected override void SetupForRun()
        {
            base.SetupForRun();

            // OnActivate / OnDeactivate may not fire, so the initial activity state may be unknown here.
            // In order to be certain we have the correct activity state we are querying the Windows API here.

            timePeriod = new TimePeriod(1);
        }

        protected override IWindow CreateWindow(GraphicsSurfaceType preferredSurface)
            => FrameworkEnvironment.UseSDL3
                ? new SDL3WindowsWindow(preferredSurface, Options.FriendlyGameName)
                : new SDL2WindowsWindow(preferredSurface, Options.FriendlyGameName);

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
            setGamePriority(true);
            base.OnActivated();
        }

        protected override void OnDeactivated()
        {
            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous);
            setGamePriority(false);
            base.OnDeactivated();
        }

        private void setGamePriority(bool active)
        {
            try
            {
                // We set process priority after the window becomes active, because for whatever reason windows will
                // reset this when the window becomes active after being inactive when game mode is enabled.
                Process.GetCurrentProcess().PriorityClass = active ? ProcessPriorityClass.High : ProcessPriorityClass.Normal;
            }
            catch
            {
                // Failure to set priority is not critical.
            }
        }
    }
}
