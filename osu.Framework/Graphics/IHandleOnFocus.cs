// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics
{
    public interface IHandleOnFocus
    {
        /// <summary>
        /// Triggered whenever this Drawable gains focus.
        /// Focused Drawables receive keyboard input before all other Drawables,
        /// and thus handle it first.
        /// </summary>
        /// <param name="state">The state after focus when focus can be gained.</param>
        void OnFocus(InputState state);
    }
}
