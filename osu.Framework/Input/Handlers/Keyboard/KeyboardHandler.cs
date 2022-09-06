// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Input.Handlers.Keyboard
{
    public class KeyboardHandler : InputHandler
    {
        public override string Description => "Keyboard";

        public override bool IsActive => true;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (!(host.Window is SDL2DesktopWindow window))
                return false;

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    window.KeyDown += handleKeyDown;
                    window.KeyUp += handleKeyUp;
                }
                else
                {
                    window.KeyDown -= handleKeyDown;
                    window.KeyUp -= handleKeyUp;
                }
            }, true);

            return true;
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.KeyEvents);
        }

        private void handleKeyDown(KeyboardKey key) => enqueueInput(new KeyboardKeyInput(key, true));

        private void handleKeyUp(KeyboardKey key) => enqueueInput(new KeyboardKeyInput(key, false));
    }
}
