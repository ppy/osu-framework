﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.Handlers.Touchpad;
using osu.Framework.Platform.Windows.Native;
using osuTK;
using SDL2;

namespace osu.Framework.Platform.Windows
{
    public class WindowsGameHost : DesktopGameHost
    {
        private TimePeriod timePeriod;

        public override Clipboard GetClipboard() => new WindowsClipboard();

        public override string UserStoragePath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

#if NET5_0
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        public override bool CapsLockEnabled => Console.CapsLock;

        private readonly SDL.SDL_WindowsMessageHook hook;

        internal WindowsGameHost(string gameName, bool bindIPC = false, ToolkitOptions toolkitOptions = default, bool portableInstallation = false)
            : base(gameName, bindIPC, portableInstallation)
        {
            hook = (userdata, hwnd, message, wparam, lparam) =>
            {
                IntPtr returnCode = IntPtr.Zero;
                OnWndProc?.Invoke(userdata, hwnd, message, wparam, lparam, ref returnCode);
                return returnCode;
            };
        }

        public override void OpenFileExternally(string filename)
        {
            if (Directory.Exists(filename))
            {
                Process.Start("explorer.exe", filename);
                return;
            }

            base.OpenFileExternally(filename);
        }

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
        {
            return base.CreateAvailableInputHandlers()
                       .Select(inputHandler => inputHandler is TouchpadHandler ? new WindowsTouchpadHandler() : inputHandler)
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
            InputThread.Scheduler.Add(() => SDL.SDL_SetWindowsMessageHook(hook, IntPtr.Zero));
            base.OnActivated();
        }

        protected override void OnDeactivated()
        {
            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous);
            InputThread.Scheduler.Add(() => SDL.SDL_SetWindowsMessageHook(null, IntPtr.Zero));
            base.OnDeactivated();
        }

        public delegate void WindowsMessageHook(IntPtr userdata, IntPtr hwnd, uint message, ulong wparm, long lparam, ref IntPtr returnCode);

        public event WindowsMessageHook OnWndProc;
    }
}
