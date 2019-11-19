// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using Veldrid;
using TKKey = osuTK.Input.Key;

namespace osu.Framework.Input.Handlers.Keyboard
{
    public class Sdl2KeyboardHandler : InputHandler
    {
        private readonly KeyboardState lastKeyboardState = new KeyboardState();
        private readonly KeyboardState thisKeyboardState = new KeyboardState();

        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host)
        {
            if (!(host.Window is Window window))
                return false;

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    window.KeyDown += handleKeyboardEvent;
                    window.KeyUp += handleKeyboardEvent;
                }
                else
                {
                    window.KeyDown -= handleKeyboardEvent;
                    window.KeyUp -= handleKeyboardEvent;
                }
            }, true);

            return true;
        }

        private void handleKeyboardEvent(KeyEvent keyEvent)
        {
            thisKeyboardState.Keys.SetPressed((TKKey)keyEvent.Key, keyEvent.Down);
            PendingInputs.Enqueue(new KeyboardKeyInput(thisKeyboardState.Keys, lastKeyboardState.Keys));
            lastKeyboardState.Keys.SetPressed((TKKey)keyEvent.Key, keyEvent.Down);
            FrameStatistics.Increment(StatisticsCounterType.KeyEvents);
        }
    }
}
