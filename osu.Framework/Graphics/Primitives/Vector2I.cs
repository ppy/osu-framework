// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Primitives
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Vector2I : IEquatable<Vector2I>
    {
        public int X;
        public int Y;

        public Vector2I(int val) : this(val, val)
        {
        }

        public Vector2I(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public static readonly Vector2I ZERO = new Vector2I();

        public static readonly Vector2I ONE = new Vector2I(1, 1);

        public static implicit operator Vector2(Vector2I r) => new Vector2(r.X, r.Y);

        public static bool operator ==(Vector2I left, Vector2I right) => left.Equals(right);

        public static bool operator !=(Vector2I left, Vector2I right) => !(left == right);

        public static Vector2I operator +(Vector2I left, Vector2I right) => new Vector2I(left.X + right.X, left.Y + right.Y);

        public static Vector2I operator -(Vector2I left, Vector2I right) => new Vector2I(left.X - right.X, left.Y - right.Y);

        public bool Equals(Vector2I other) => other.X == X && other.Y == Y;

        public override bool Equals(object obj)
        {
            if (!(obj is Vector2I))
                return false;
            return Equals((Vector2I)obj);
        }

        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            return (int)((uint)X ^ (uint)Y << 13 | (uint)Y >> 0x13);
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }
    }
}
