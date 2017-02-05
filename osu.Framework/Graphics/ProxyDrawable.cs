// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics
{
    public class ProxyDrawable : Drawable
    {
        internal Drawable Original;

        public ProxyDrawable(Drawable original)
        {
            Original = original;
        }

        public override bool IsPresent => false;
    }
}