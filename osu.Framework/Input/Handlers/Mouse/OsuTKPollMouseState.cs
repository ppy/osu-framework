// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osuTK;

namespace osu.Framework.Input.Handlers.Mouse
{
    /// <summary>
    /// An osuTK state which was retrieved via polling.
    /// </summary>
    internal class OsuTKPollMouseState : OsuTKMouseState
    {
        public OsuTKPollMouseState(osuTK.Input.MouseState tkState, bool active, Vector2? mappedPosition)
            : base(tkState, active, mappedPosition)
        {
        }
    }
}
