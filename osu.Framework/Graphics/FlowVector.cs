// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics
{
    /// <summary>
    /// A <see cref="osuTK.Vector2"/> with an arbitrary orientation represented as "Flow" - the main axis
    /// and "Line" - the secondary axis, perpendicular to "Flow".
    /// For example, in a text container, in the left-right/top-bottom orientation
    /// "Flow" would be used for the +X axis (right) and "Line" for the +Y axis (down).
    /// If we wanted to make the text container now handle a right-left/top-bottom orientation,
    /// all we would need to do is interpret the "Flow" axis as -X (left) instead.
    /// </summary>
    public struct FlowVector
    {
        public float Flow;
        public float Line;

        public FlowVector(float flow, float line)
        {
            Flow = flow;
            Line = line;
        }

        public FlowVector(float both)
        {
            Flow = both;
            Line = both;
        }

        public static FlowVector Zero { get; } = new FlowVector();
        public static FlowVector One { get; } = new FlowVector(1);

        public static FlowVector operator +(FlowVector a, FlowVector b)
            => new FlowVector(a.Flow + b.Flow, a.Line + b.Line);

        public static FlowVector operator -(FlowVector a, FlowVector b)
            => new FlowVector(a.Flow - b.Flow, a.Line - b.Line);

        public static FlowVector operator *(FlowVector a, FlowVector b)
            => new FlowVector(a.Flow * b.Flow, a.Line * b.Line);
    }
}
