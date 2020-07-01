// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input.StateChanges
{
    public interface IMouseInput : IInput
    {
        /// <summary>
        /// Whether this input is performed from a primary touch input source.
        /// </summary>
        public bool FromTouchSource { get; set; }
    }
}
