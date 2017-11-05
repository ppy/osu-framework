// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics
{
    public interface IHandleOnFocusLost
    {
        /// <summary>
        /// Triggered whenever this Drawable lost focus.
        /// </summary>
        /// <param name="state">The state after focus was lost.</param>
        void OnFocusLost(InputState state);
    }
}
