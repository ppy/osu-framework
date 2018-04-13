// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input.Handlers.Mouse
{
    /// <summary>
    /// An OpenTK state which came from an event callback.
    /// </summary>
    internal class OpenTKEventMouseState : OpenTKMouseState
    {
        public OpenTKEventMouseState(OpenTK.Input.MouseState tkState, bool active, Vector2? mappedPosition)
            : base(tkState, active, mappedPosition)
        {
        }
    }
}
