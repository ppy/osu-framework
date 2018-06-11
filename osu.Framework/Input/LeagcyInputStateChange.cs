// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input
{
    /// <summary>
    /// Only temporary class for the WIP. Doesn't work for classes that is derived from InputState.
    /// </summary>
    public class LeagcyInputStateChange : IInput
    {
        public InputState InputState;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            // it is like createDistinctStates
            
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

                var (released, pressesd) = state.Mouse.Buttons.EnumerateDifference(InputState.Mouse.Buttons);

                foreach (var button in released)
                    handler.HandleMouseButtonStateChange(state, button, ButtonStateChangeKind.Released);

                foreach (var button in pressesd)
                    handler.HandleMouseButtonStateChange(state, button, ButtonStateChangeKind.Pressed);

                state.Mouse.Buttons.Set(InputState.Mouse.Buttons);
            }

            if (InputState.Keyboard != null)
            {
                var (released, pressesd) = state.Keyboard.Keys.EnumerateDifference(InputState.Keyboard.Keys);

                foreach (var button in released)
                    handler.HandleKeyboardKeyStateChange(state, button, ButtonStateChangeKind.Released);

                foreach (var button in pressesd)
                    handler.HandleKeyboardKeyStateChange(state, button, ButtonStateChangeKind.Pressed);

                state.Keyboard.Keys.Set(InputState.Keyboard.Keys);
            }

            // todo: joystick
        }
    }
}
