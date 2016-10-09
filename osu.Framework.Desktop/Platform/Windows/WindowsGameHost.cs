// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using osu.Framework.Desktop.Input.Handlers.Keyboard;
using osu.Framework.Desktop.Input.Handlers.Mouse;
using osu.Framework.Desktop.Platform.Windows.Native;
using osu.Framework.Input.Handlers;
using OpenTK.Graphics;
using NativeWindow = OpenTK.NativeWindow;

namespace osu.Framework.Desktop.Platform.Windows
{
    public class WindowsGameHost : DesktopGameHost
    {
        private TimePeriod timePeriod;

        internal WindowsGameHost(GraphicsContextFlags flags)
        {
            IsActive = Window != null && GetForegroundWindow().Equals(Window.Handle);

            timePeriod = new TimePeriod(1) { Active = true };

            Architecture.SetIncludePath();

            Window = new WindowsGameWindow(flags);
            Window.Activated += OnActivated;
            Window.Deactivated += OnDeactivated;

            Application.EnableVisualStyles();
        }

        public override IEnumerable<InputHandler> GetInputHandlers()
        {
            //todo: figure why opentk input handlers aren't working.
            return new InputHandler[] {
                new CursorMouseHandler(), //handles cursor position
                new FormMouseHandler(),   //handles button states
                new FormKeyboardHandler(),
            };
        }

        protected override void Dispose(bool isDisposing)
        {
            timePeriod.Dispose();
            base.Dispose(isDisposing);
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            timePeriod.Active = true;

            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous | Execution.ExecutionState.SystemRequired | Execution.ExecutionState.DisplayRequired);
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            timePeriod.Active = false;

            Execution.SetThreadExecutionState(Execution.ExecutionState.Continuous);
            base.OnDeactivated(sender, args);
        }

        public override void Run()
        {
            //not sure this is still needed
            NativeWindow.OsuWindowHandle = Window.Handle;

            base.Run();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
    }
}
