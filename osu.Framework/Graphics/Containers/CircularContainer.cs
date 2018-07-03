// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Caching;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which is rounded (via automatic corner-radius) on the shortest edge.
    /// </summary>
    public class CircularContainer : Container
    {
        private Cached cornerRadius = new Cached();

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            bool result = base.Invalidate(invalidation, source, shallPropagate);

            if ((invalidation & (Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit)) > 0)
                cornerRadius.Invalidate();

            return result;
        }

        protected override void UpdateAfterAutoSize()
        {
            base.UpdateAfterAutoSize();

            if (!cornerRadius.IsValid)
            {
                CornerRadius = Math.Min(DrawSize.X, DrawSize.Y) / 2f;
                cornerRadius.Validate();
            }
        }
    }
}
