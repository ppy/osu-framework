// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform;

namespace osu.Framework.Input.Handlers.Mouse
{
    public class MouseHandler : InputHandler
    {
        private Window window;

        public override bool Initialize(GameHost host)
        {
            window = host.Window as Window;

            if (window == null)
                return false;

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    window.MouseMove += PendingInputs.Enqueue;
                    window.MouseDown += PendingInputs.Enqueue;
                    window.MouseUp += PendingInputs.Enqueue;
                    window.MouseWheel += PendingInputs.Enqueue;
                }
                else
                {
                    window.MouseMove -= PendingInputs.Enqueue;
                    window.MouseDown -= PendingInputs.Enqueue;
                    window.MouseUp -= PendingInputs.Enqueue;
                    window.MouseWheel -= PendingInputs.Enqueue;
                }
            }, true);

            return true;
        }

        public override bool IsActive => true;

        public override int Priority => 0;
    }
}
