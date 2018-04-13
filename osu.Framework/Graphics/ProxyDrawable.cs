// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics
{
    public class ProxyDrawable : Drawable
    {
        public ProxyDrawable(Drawable original)
        {
            Original = original;
        }

        internal sealed override Drawable Original { get; }

        public override bool RemoveWhenNotAlive => base.RemoveWhenNotAlive && Original.RemoveWhenNotAlive;

        protected internal override bool ShouldBeAlive => base.ShouldBeAlive && Original.ShouldBeAlive;

        // We do not want to receive updates. That is the business
        // of the original drawable.
        public override bool IsPresent => false;

        public override bool UpdateSubTreeMasking(Drawable source, RectangleF maskingBounds) => Original.UpdateSubTreeMasking(this, maskingBounds);
    }
}
