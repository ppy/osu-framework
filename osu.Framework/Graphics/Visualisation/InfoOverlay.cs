// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Visualisation
{
    class InfoOverlay : Container
    {
        private Drawable target;
        public Drawable Target
        {
            get
            {
                return target;
            }

            set
            {
                if (target != null)
                    target.OnInvalidate -= update;

                target = value;
                box.Target = target;

                if (target != null)
                    target.OnInvalidate += update;
            }
        }

        private FlashyBox box;

        public InfoOverlay()
        {
            RelativeSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                box = new FlashyBox(),
            };
        }

        private void update()
        {
            box.Invalidate(Invalidation.DrawNode);
            if (!target.IsAlive)
                Expire();
        }
    }
}