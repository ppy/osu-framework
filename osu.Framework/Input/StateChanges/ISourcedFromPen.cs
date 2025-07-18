// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes a simulated mouse input that was made by a tablet/pen device.
    /// </summary>
    // todo: this is not ready to be used externally for distinguishing input, therefore it's internal for now.
    internal interface ISourcedFromPen : IInput
    {
        /// <summary>
        /// The type of the tablet or pen device that made this input.
        /// </summary>
        TabletPenDeviceType DeviceType { get; }
    }
}
