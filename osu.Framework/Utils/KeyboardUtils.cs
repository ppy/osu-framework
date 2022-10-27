// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;

namespace osu.Framework.Utils
{
    public abstract class KeyboardUtils
    {
        /// <summary>
        /// The true target height of the keyboard (even when undocked on iOS)
        /// </summary>
        public double TrueHeight = 0;

        /// <summary>
        /// The target height of the keyboard that can be used safely
        /// </summary>
        public double Height = 0;

        /// <summary>
        /// If the keyboard is docked (iOS)
        /// </summary>
        public bool Docked = true;

        /// <summary>
        /// If the keyboard is visible
        /// </summary>
        public bool Visible = false;

        /// <summary>
        /// The animation duration of the keyboard
        /// (-1 if the keyboard is not being animated)
        /// </summary>
        public double AnimationDuration = -1;

        /// <summary>
        /// The type of transition. (Linear, EaseInOut, etc.)
        /// </summary>
        public Easing AnimationType = Easing.InOutSine;
    }
}
