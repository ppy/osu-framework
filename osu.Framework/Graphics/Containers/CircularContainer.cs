// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which is rounded (via automatic corner-radius and corner-exponent=2) on the shortest edge.
    /// </summary>
    public class CircularContainer : Container
    {
        internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
        {
            // this shouldn't have to be done here, but it's the only place it works correctly.
            // see https://github.com/ppy/osu-framework/pull/1666
            CornerRadius = Math.Min(DrawSize.X, DrawSize.Y) / 2f;

            return base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);
        }
    }
}
