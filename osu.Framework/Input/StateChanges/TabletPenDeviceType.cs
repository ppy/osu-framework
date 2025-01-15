// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input.StateChanges
{
    public enum TabletPenDeviceType
    {
        /// <summary>
        /// This tablet device might be <see cref="Direct"/> or <see cref="Indirect"/>, the input handler doesn't have enough information to decide.
        /// </summary>
        Unknown,

        /// <summary>
        /// A tablet device with a built-in display.
        /// </summary>
        /// <remarks>
        /// The pen is physically pointing at the screen, so it's unnecessary to show a cursor.
        /// </remarks>
        Direct,

        /// <summary>
        /// An indirect tablet device, the pen is not pointing at a screen, but a separate surface.
        /// </summary>
        /// <remarks>
        /// You may want to show a cursor so the user can see where the pen is pointing.
        /// </remarks>
        Indirect
    }
}
