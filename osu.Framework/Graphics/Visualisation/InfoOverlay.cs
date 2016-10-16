// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Visualisation
{
    class InfoOverlay : Container
    {
        private Drawable target;

        private Box tl, tr, bl, br;

        public InfoOverlay(Drawable target)
        {
            this.target = target;
            target.OnInvalidate += update;

            RelativeSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                tl = new FlashyBox(),
                tr = new FlashyBox(),
                bl = new FlashyBox(),
                br = new FlashyBox()
            };
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);
            update();
        }

        private void update()
        {
            Quad q = target.ScreenSpaceDrawQuad * DrawInfo.MatrixInverse;

            tl.Position = q.TopLeft;
            tr.Position = q.TopRight;
            bl.Position = q.BottomLeft;
            br.Position = q.BottomRight;

            if (!target.IsAlive)
                Expire();
        }
    }
}