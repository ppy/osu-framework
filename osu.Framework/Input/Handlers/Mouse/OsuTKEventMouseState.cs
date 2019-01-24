// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
