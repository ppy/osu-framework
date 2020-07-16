// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges.Events;

namespace osu.Framework.Input.StateChanges
{
    public interface ISourcedFromTouch
    {
        TouchStateChangeEvent TouchEvent { get; set; }
    }
}
