// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input.StateChanges
{
    public class MousePositionRelativeInputFromPen : MousePositionRelativeInput, ISourcedFromPen
    {
        public required TabletPenDeviceType DeviceType { get; init; }
    }
}
