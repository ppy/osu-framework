// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input.Handlers;

namespace osu.Framework.Desktop.Platform
{
    /// <summary>
    /// A GameHost which doesn't require a graphical or sound device.
    /// </summary>
    public class HeadlessGameHost : DesktopGameHost
    {
        public HeadlessGameHost(string gameName = @"", bool bindIPC = false)
            : base(gameName, bindIPC)
        {
            UpdateThread.Scheduler.Update();
            Dependencies.Cache(Storage = new DesktopStorage(string.Empty));
        }

        protected override void UpdateInitialize()
        {
        }

        protected override void DrawInitialize()
        {
        }

        protected override void DrawFrame()
        {
            //we can't draw.
        }

        public override IEnumerable<InputHandler> GetInputHandlers() => new InputHandler[] { };
    }
}
