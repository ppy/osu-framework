// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using TKKey = osuTK.Input.Key;

namespace osu.Framework.Input.Handlers.Keyboard
{
    public class KeyboardHandler : InputHandler
    {
        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host)
        {
            if (!(host.Window is DesktopWindow window))
                return false;

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    window.KeyDown += handleKeyPress;
                    window.KeyUp += handleKeyPress;
                }
                else
                {
                    window.KeyDown -= handleKeyPress;
                    window.KeyUp -= handleKeyPress;
                }
            }, true);

            return true;
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.KeyEvents);
        }

        private void handleKeyPress(KeyPressInputArgs args) => enqueueInput(new KeyboardKeyInput(args.Key, args.Pressed));
    }
}
