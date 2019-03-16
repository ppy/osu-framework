// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public abstract class FocusEventBase : UIEvent
    {
        protected FocusEventBase([NotNull] InputState state)
            : base(state)
        {
        }
    }
}
