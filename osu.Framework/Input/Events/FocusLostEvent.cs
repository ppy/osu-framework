// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// An event represeting that a drawable lost the focus.
    /// </summary>
    public class FocusLostEvent : UIEvent
    {
        public FocusLostEvent(InputState state)
            : base(state)
        {
        }
    }
}
