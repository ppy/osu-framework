// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osuTK;

namespace osu.Framework.Input.Handlers.Mouse
{
    /// <summary>
    /// An osuTK state which came from an event callback.
    /// </summary>
    internal class OsuTKEventMouseState : OsuTKMouseState
    {
        public OsuTKEventMouseState(osuTK.Input.MouseState tkState, bool active, Vector2? mappedPosition)
            : base(tkState, active, mappedPosition)
        {
        }
    }
}
