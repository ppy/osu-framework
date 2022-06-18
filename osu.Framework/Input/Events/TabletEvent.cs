// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public abstract class TabletEvent : UIEvent
    {
        protected TabletEvent([NotNull] InputState state)
            : base(state)
        {
        }

        /// <summary>
        /// List of currently pressed tablet pen buttons.
        /// </summary>
        public IEnumerable<TabletPenButton> PressedPenButtons => CurrentState.Tablet.PenButtons;

        /// <summary>
        /// List of currently pressed auxiliary buttons.
        /// </summary>
        /// <value></value>
        public IEnumerable<TabletAuxiliaryButton> PressedAuxiliaryButtons => CurrentState.Tablet.AuxiliaryButtons;
    }
}
