// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.StateChanges.Events;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes an input which was sourced from a touch event.
    /// Generally used to mark when an alternate input was triggered from a touch source (ie. touch being emulated as a mouse).
    /// </summary>
    public interface ISourcedFromTouch : IInput
    {
        /// <summary>
        /// The source touch event.
        /// </summary>
        TouchStateChangeEvent TouchEvent { get; }
    }
}
