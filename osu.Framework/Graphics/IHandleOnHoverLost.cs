// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.Graphics
{
    public interface IHandleOnHoverLost
    {
        /// <summary>
        /// Triggered whenever this drawable is no longer hovered.
        /// </summary>
        /// <param name="state">The state at which hover is lost.</param>
        void OnHoverLost(InputState state);
    }
}
