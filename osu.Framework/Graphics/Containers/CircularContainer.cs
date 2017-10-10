// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which is rounded (via automatic corner-radius) on the shortest edge.
    /// </summary>
    public class CircularContainer : Container
    {
        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            bool result = base.Invalidate(invalidation, source, shallPropagate);

            if ((invalidation & (Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit)) > 0)
                CornerRadius = Math.Min(DrawSize.X, DrawSize.Y) / 2f;

            return result;
        }
    }
}
