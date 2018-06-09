// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Input
{
    /// <summary>
    /// Only temporary class for the WIP. Doesn't work for classes that is derived from InputState.
    /// </summary>
    public class LeagcyInputStateChange : IInputHandlerResult
    {
        public InputState InputState;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            // it is like createDistinctStates


            void processForButtons<TButton>(
                IReadOnlyList<TButton> lastButtons,
                IEnumerable<TButton> incomingButtons,
                Action<IReadOnlyList<TButton>, TButton, ButtonStateChangeKind> action)
                where TButton : struct
            {
                foreach (var releasedButton in lastButtons.Except(incomingButtons))
                {
                    lastButtons = lastButtons.Where(d => !d.Equals(releasedButton)).ToArray();
                    action(lastButtons, releasedButton, ButtonStateChangeKind.Released);
                }

                foreach (var pressedButton in incomingButtons.Except(lastButtons))
                {
                    lastButtons = lastButtons.Union(new[] { pressedButton }).ToArray();
                    action(lastButtons, pressedButton, ButtonStateChangeKind.Pressed);
                }
            }


            if (InputState.Mouse != null)
            {
                if (state.Mouse.Position != InputState.Mouse.Position)
                {
                    state.Mouse.Position = InputState.Mouse.Position;
                    handler.HandleMousePositionChange(state);
                }

                if (state.Mouse.Scroll != InputState.Mouse.Scroll)
                {
                    state.Mouse.Scroll = InputState.Mouse.Scroll;
                    handler.HandleMouseScrollChange(state);
                }

                processForButtons(state.Mouse.Buttons.ToArray(), InputState.Mouse.Buttons, (buttons, button, kind) =>
                {
                    state.Mouse.Buttons = buttons;
                    handler.HandleMouseButtonChange(state, button, kind);
                });
            }

            if (InputState.Keyboard != null)
            {
                processForButtons(state.Keyboard.Keys.ToArray(), InputState.Keyboard.Keys, (buttons, button, kind) =>
                {
                    state.Keyboard = new KeyboardState { Keys = buttons };
                    handler.HandleKeyboardChange(state, button, kind);
                });
            }

            // todo: joystick
        }
    }
}
