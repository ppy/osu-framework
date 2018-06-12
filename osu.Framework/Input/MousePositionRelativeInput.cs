// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input
{
    /// <summary>
    /// Denotes a relative change of mouse position.
    /// <para>It can be used to denote a move of a relatively positioned devices such as mouses.</para>
    /// </summary>
    public class MousePositionRelativeInput : IInput
    {
        /// <summary>
        /// The delta value which will be added to the current mouse position.
        /// When the current mouse position is not valid, no changes will be made.
        /// </summary>
        public Vector2 Delta;
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (state.Mouse.IsPositionValid && Delta != Vector2.Zero)
            {
                state.Mouse.Position += Delta;
                handler.HandleMousePositionChange(state);
            }
        }
    }
}
