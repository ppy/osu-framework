// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;

namespace osu.Framework.Graphics
{
    public struct MarginPadding : IEquatable<MarginPadding>
    {
        public float Top;
        public float Left;
        public float Bottom;
        public float Right;

        public float TotalHorizontal => Left + Right;

        public float TotalVertical => Top + Bottom;

        public Vector2 Total => new Vector2(TotalHorizontal, TotalVertical);

        public MarginPadding(float allSides)
        {
            Top = Left = Bottom = Right = allSides;
        }

        public void ThrowIfNegative()
        {
            if (Top < 0 || Left < 0 || Bottom < 0 || Right < 0)
                throw new InvalidOperationException($"{nameof(MarginPadding)} may not have negative values, but values are {this}.");
        }

        public bool Equals(MarginPadding other)
        {
            return Top == other.Top && Left == other.Left && Bottom == other.Bottom && Right == other.Right;
        }

        public static MarginPadding operator -(MarginPadding mp)
        {
            return new MarginPadding
            {
                Left = -mp.Left,
                Top = -mp.Top,
                Right = -mp.Right,
                Bottom = -mp.Bottom,
            };
        }
    }
}
