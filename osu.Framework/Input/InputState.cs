// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class InputState : EventArgs
    {
        public IKeyboardState Keyboard;
        public IMouseState Mouse;
        public InputState Last;

        public virtual InputState Clone()
        {
            var clone = (InputState)MemberwiseClone();
            clone.Keyboard = Keyboard?.Clone();
            clone.Mouse = Mouse?.Clone();
            clone.Last = Last;

            return clone;
        }

        /// <summary>
        /// In order to provide a reliable event system to drawables, we want to ensure that we reprocess input queues (via the
        /// main loop in <see cref="InputManager"/> after each and every button or key change. This allows
        /// correct behaviour in a case where the input queues change based on triggered by a button or key.
        /// </summary>
        /// <param name="currentState">The current InputState from which distinct states will be computed.</param>
        /// <returns>Processed states such that at most one attribute change occurs between any two consecutive states.</returns>
        public virtual IEnumerable<InputState> CreateDistinctStates(InputState currentState)
        {
            IKeyboardState lastKeyboard = currentState.Keyboard;
            IMouseState lastMouse = currentState.Mouse;

            InputState state;

            if (Mouse != null)
            {
                // first we want to create a copy of ourselves without any button changes
                // this is done only for mouse handlers, as they have positional data we want to handle in a separate pass.
                var iWithoutButtons = Mouse.Clone();

                for (MouseButton b = 0; b < MouseButton.LastButton; b++)
                    iWithoutButtons.SetPressed(b, lastMouse?.IsPressed(b) ?? false);

                //we start by adding this state to the processed list...
                state = Clone();
                state.Mouse = iWithoutButtons;
                yield return state;

                lastMouse = iWithoutButtons;

                //and then iterate over each button/key change, adding intermediate states along the way.
                for (MouseButton b = 0; b < MouseButton.LastButton; b++)
                {
                    if (Mouse.IsPressed(b) != (lastMouse?.IsPressed(b) ?? false))
                    {
                        var intermediateState = lastMouse?.Clone() ?? new MouseState();

                        //add our single local change
                        intermediateState.SetPressed(b, Mouse.IsPressed(b));

                        lastMouse = intermediateState;

                        state = Clone();
                        state.Mouse = intermediateState;
                        yield return state;
                    }
                }
            }

            if (Keyboard != null)
            {
                if (lastKeyboard != null)
                    foreach (var releasedKey in lastKeyboard.Keys.Except(Keyboard.Keys))
                    {
                        state = Clone();
                        state.Keyboard = lastKeyboard = new KeyboardState { Keys = lastKeyboard.Keys.Where(d => d != releasedKey).ToArray() };
                        yield return state;
                    }

                foreach (var pressedKey in Keyboard.Keys.Except(lastKeyboard?.Keys ?? new Key[] { }))
                {
                    state = Clone();
                    state.Keyboard = lastKeyboard = new KeyboardState { Keys = lastKeyboard?.Keys.Union(new[] { pressedKey }) ?? new [] { pressedKey } };
                    yield return state;
                }
            }
        }
    }
}
