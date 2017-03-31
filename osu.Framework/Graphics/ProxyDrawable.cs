// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics
{
    public class ProxyDrawable : Drawable
    {
        public ProxyDrawable(Drawable original)
        {
            Original = original;
        }

        internal sealed override Drawable Original { get; }

        public override bool IsAlive => base.IsAlive && Original.IsAlive;

        // We do not want to receive updates. That is the business
        // of the original drawable.
        public override bool IsPresent => false;
    }
}
