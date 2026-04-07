// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Window has touchpad input reported
    /// </summary>
    internal interface IHasTouchpadInput : IWindow
    {
        /// <summary>
        /// Publish the touchpad data. Read by <see cref="Input.Handlers.Touchpad.TouchpadHandler"/>.
        /// </summary>
        public event Action<TouchpadData>? TouchpadDataUpdate;
    }

    /// <summary>Information for the whole touchpad.</summary>
    public struct TouchpadData
    {
        /// <summary>Static information.</summary>
        public readonly TouchpadInfo Info;

        /// <summary>Valid touch points.</summary>
        public readonly List<TouchpadPoint> Points;

        /// <summary>Is the touchpad pressed down?</summary>
        public readonly bool ButtonDown;

        public TouchpadData(TouchpadInfo info, List<TouchpadPoint> points, bool buttonDown)
        {
            Info = info;
            Points = points;
            ButtonDown = buttonDown;
        }
    }

    /// <summary>Information for the whole touchpad.</summary>
    public struct TouchpadInfo
    {
        /// <summary>Arbitrary numerical value to differentiate individual touchpads.</summary>
        public IntPtr Handle;

        /// <summary>The limit for the raw XY values.</summary>
        /// <remarks>
        /// <para>To map the values to 0~1, use `(value-min)/range`.</para>
        /// <para>The YRange/XRange value is the aspect ratio of the touchpad.</para>
        /// </remarks>
        public int XMin, YMin, XRange, YRange;
    }

    /// <summary>Information for every touch point.</summary>
    public struct TouchpadPoint
    {
        public int X, Y;

        /// <summary>Unique ID for the contact.</summary>
        /// <remarks>
        /// The position of one contact in the array may and will change, if fingers are added or removed.
        /// </remarks>
        public int ContactId;

        /// <summary>Is finger in contact with the touchpad?</summary>
        /// <remarks>
        /// If false, the XY coordinate may still be valid, but the finger can be in a hover state.
        /// </remarks>
        public bool Valid;

        /// <summary>Is touch point too large to be considered invalid?</summary>
        /// <remarks>
        /// If false, this contact may be a palm-touchpad contact.
        /// </remarks>
        public bool Confidence;
    }
}
