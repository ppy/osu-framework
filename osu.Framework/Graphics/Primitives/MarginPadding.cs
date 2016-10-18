﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Primitives
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

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public bool Equals(MarginPadding other)
        {
            return Top == other.Top && Left == other.Left && Bottom == other.Bottom && Right == other.Right;
        }
    }
}
