// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which is rounded (via automatic corner-radius) on the shortest edge.
    /// </summary>
    public class CircularMaskedContainer : Container
    {
        public CircularMaskedContainer()
        {
            Masking = true;
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if (!Masking) throw new InvalidOperationException($@"{nameof(CircularMaskedContainer)} must always have masking applied");
            return base.Invalidate(invalidation, source, shallPropagate);
        }

        public override float CornerRadius
        {
            get
            {
                return Math.Min(DrawSize.X, DrawSize.Y) / 2f;
            }

            set
            {
                throw new InvalidOperationException($"Cannot manually set {nameof(CornerRadius)} of {nameof(CircularMaskedContainer)}.");
            }
        }
    }
}
