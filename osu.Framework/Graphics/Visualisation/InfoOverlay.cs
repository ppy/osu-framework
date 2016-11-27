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
                target = value;
                box.Target = target;

                if (target != null)
                    Alpha = 1;
                else
                    Alpha = 0;
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

        protected override void Update()
        {
            base.Update();
            box.Invalidate(Invalidation.DrawNode);
        }
    }
}