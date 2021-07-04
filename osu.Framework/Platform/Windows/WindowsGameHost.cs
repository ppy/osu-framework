// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.Input.Handlers.Keyboard;
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

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[]
            {
                new KeyboardHandler(),
#if NET5_0
                // tablet should get priority over mouse to correctly handle cases where tablet drivers report as mice as well.
                new Input.Handlers.Tablet.OpenTabletDriverHandler(),
#endif
                // todo: while this does enable trackpad, it also breaks scrolling functionaliy.
                // todo: the best way would probably only gave trackpad during in game.
                new WindowsTrackpadHandler(),
                new WindowsMouseHandler(),
                new JoystickHandler(),
                new MidiHandler(),
            };

        protected override void SetupForRun()
        {
            base.SetupForRun();

            // OnActivate / OnDeactivate may not fire, so the initial activity state may be unknown here.
            // In order to be certain we have the correct activity state we are querying the Windows API here.

            timePeriod = new TimePeriod(1) { Active = true };
        }

        protected override IWindow CreateWindow() => new WindowsWindow();

        public override IEnumerable<KeyBinding> PlatformKeyBindings => base.PlatformKeyBindings.Concat(new[]
        {
            new KeyBinding(new KeyCombination(InputKey.Alt, InputKey.F4), new PlatformAction(PlatformActionType.Exit))
        }).ToList();

        protected override void Dispose(bool isDisposing)
        {
            timePeriod?.Dispose();
            base.Dispose(isDisposing);
        }

        protected override void OnActivated()
        {
            timePeriod.Active = true;

            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous | Execution.ExecutionState.SystemRequired | Execution.ExecutionState.DisplayRequired);
            InputThread.Scheduler.Add(() => SDL.SDL_SetWindowsMessageHook(hook, IntPtr.Zero));
            base.OnActivated();
        }

        protected override void OnDeactivated()
        {
            timePeriod.Active = false;

            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous);
            InputThread.Scheduler.Add(() => SDL.SDL_SetWindowsMessageHook(null, IntPtr.Zero));
            base.OnDeactivated();
        }

        public delegate void WindowsMessageHook(IntPtr userdata, IntPtr hwnd, uint message, ulong wparm, long lparam, ref IntPtr returnCode);

        public event WindowsMessageHook OnWndProc;
    }
}
