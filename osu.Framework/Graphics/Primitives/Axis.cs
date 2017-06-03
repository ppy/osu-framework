// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Graphics.Primitives
{
    /// <summary>
    /// An axis formed between two points.
    /// </summary>
    public struct Axis
    {
        public readonly Vector2 Edge;
        public readonly Vector2 Normal;

        public Axis(Vector2 firstPoint, Vector2 secondPoint)
        {
            Edge = secondPoint - firstPoint;
            Normal = new Vector2(-Edge.Y, Edge.X);
        }

        public static bool operator ==(Axis left, Axis right)
        {
            return left.Edge == right.Edge;
        }

        public static bool operator !=(Axis left, Axis right)
        {
            return left.Edge != right.Edge;
        }

        public bool Equals(Axis other)
        {
            return Edge.Equals(other.Edge);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Axis && Equals((Axis)obj);
        }

        public override int GetHashCode()
        {
            return Edge.GetHashCode();
        }
    }
}