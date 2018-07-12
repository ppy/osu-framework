// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Graphics;
using osu.Framework.Input;
using OpenTK;

namespace osu.Framework.Event
{
    /// <summary>
    /// Denotes an user interface event.
    /// </summary>
    public abstract class UIEvent : InputEvent
    {
        [CanBeNull] public Drawable Target;

        protected Vector2 ToLocalSpace(Vector2 screenSpacePosition) => Target?.Parent?.ToLocalSpace(screenSpacePosition) ?? screenSpacePosition;

        /// <summary>
        /// The current mouse position in screen space.
        /// </summary>
        public Vector2 ScreenSpaceMousePosition => InputState.Mouse.Position;

        /// <summary>
        /// The current mouse position in local space.
        /// </summary>
        public Vector2 MousePosition => ToLocalSpace(ScreenSpaceMousePosition);

        public InputState LegacyInputState
        {
            get
            {
                var state = InputState.Clone();
                state.Mouse = new LocalMouseState(InputState.Mouse.NativeState, Target);
                return state;
            }
        }

        protected UIEvent(InputState state) : base(state)
        {
        }
    }
}
