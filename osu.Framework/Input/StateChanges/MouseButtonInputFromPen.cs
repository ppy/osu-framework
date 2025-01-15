// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Input;

namespace osu.Framework.Input.StateChanges
{
    public class MouseButtonInputFromPen : MouseButtonInput, ISourcedFromPen
    {
        public MouseButtonInputFromPen(bool isPressed)
            : base(MouseButton.Left, isPressed)
        {
        }

        public required TabletPenDeviceType DeviceType { get; init; }
    }
}
