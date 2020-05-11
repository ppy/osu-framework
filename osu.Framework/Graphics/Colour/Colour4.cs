// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Colour
{
    public readonly struct Colour4
    {
        /// <summary>
        /// Represents the red component of the linear RGBA colour in the 0-1 range.
        /// </summary>
        public readonly float R;

        /// <summary>
        /// Represents the green component of the linear RGBA colour in the 0-1 range.
        /// </summary>
        public readonly float G;

        /// <summary>
        /// Represents the blue component of the linear RGBA colour in the 0-1 range.
        /// </summary>
        public readonly float B;

        /// <summary>
        /// Represents the alpha component of the RGBA colour in the 0-1 range.
        /// </summary>
        public readonly float A;

        #region Constructors

        public Colour4(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Colour4(byte r, byte g, byte b, byte a)
        {
            R = r / (float)byte.MaxValue;
            G = g / (float)byte.MaxValue;
            B = b / (float)byte.MaxValue;
            A = a / (float)byte.MaxValue;
        }

        #endregion
    }
}
